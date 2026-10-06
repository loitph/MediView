using System.Text;
using MediView.Imaging.Application.Instances;
using MediView.Imaging.Domain.Instances;

namespace MediView.Imaging.Tests;

internal sealed class FakeStudies(Guid knownStudyId) : IStudiesClient
{
    public bool StudiesServiceUp { get; set; } = true;

    public int MarkedCount { get; private set; }

    public Task<bool> CanReadAsync(Guid studyId, CancellationToken cancellationToken) =>
        Task.FromResult(studyId == knownStudyId);

    public Task<bool> TryMarkImagesImportedAsync(Guid studyId, CancellationToken cancellationToken)
    {
        if (StudiesServiceUp)
        {
            MarkedCount++;
        }

        return Task.FromResult(StudiesServiceUp);
    }
}

internal sealed class TextDicomReader : IDicomReader
{
    public static UploadedFile Dicom(string sopInstanceUid, int instanceNumber = 1) =>
        File($"{sopInstanceUid}.dcm", $"1.2.3|{sopInstanceUid}|{instanceNumber}");

    public static UploadedFile Png(string name) => File(name, "\u0089PNG");

    public async Task<DicomHeader?> ReadHeaderAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content);
        var parts = (await reader.ReadToEndAsync(cancellationToken)).Split('|');
        return parts.Length == 3 ? new DicomHeader(parts[0], parts[1], int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)) : null;
    }

    private static UploadedFile File(string name, string content) =>
        new(name, () => new MemoryStream(Encoding.UTF8.GetBytes(content)));
}

internal sealed class InMemoryBlobStore : IBlobStore
{
    private readonly Dictionary<string, byte[]> _blobs = [];

    public IReadOnlyCollection<string> Keys => _blobs.Keys;

    public async Task<long> SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _blobs.Add(key, buffer.ToArray());
        return buffer.Length;
    }

    public Stream OpenRead(string key) => new MemoryStream(_blobs[key]);

    public void Delete(string key) => _blobs.Remove(key);
}

internal sealed class InMemoryInstanceRepository : IInstanceRepository
{
    private readonly List<Instance> _saved = [];
    private readonly List<Instance> _pending = [];

    public bool FailNextSave { get; set; }

    public IReadOnlyList<Instance> Saved => _saved;

    public Task<IReadOnlySet<string>> SopInstanceUidsOfAsync(Guid studyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(_saved.Where(instance => instance.StudyId == studyId).Select(instance => instance.SopInstanceUid).ToHashSet());

    public Task<IReadOnlyList<InstanceSummary>> ListAsync(Guid studyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InstanceSummary>>(
        [
            .. _saved
                .Where(instance => instance.StudyId == studyId)
                .Select(instance => new InstanceSummary(instance.Id, instance.SeriesInstanceUid, instance.InstanceNumber)),
        ]);

    public Task<string?> FindStoragePathAsync(Guid instanceId, CancellationToken cancellationToken) =>
        Task.FromResult(_saved.SingleOrDefault(instance => instance.Id == instanceId)?.StoragePath);

    public void Add(Instance instance) => _pending.Add(instance);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            _pending.Clear();
            throw new InvalidOperationException("The database refused the save.");
        }

        _saved.AddRange(_pending);
        _pending.Clear();
        return Task.CompletedTask;
    }
}
