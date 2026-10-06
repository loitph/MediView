using MediView.Imaging.Application.Instances;

namespace MediView.Imaging.Infrastructure.Storage;

internal sealed class LocalDiskBlobStore(string root) : IBlobStore
{
    private const int BufferSize = 81_920;

    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;

    public async Task<long> SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return file.Length;
    }

    public Stream OpenRead(string key) =>
        new FileStream(PathOf(key), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);

    public void Delete(string key) => File.Delete(PathOf(key));

    private string PathOf(string key)
    {
        var path = Path.GetFullPath(key, _root);
        return path.StartsWith(_root, StringComparison.Ordinal)
            ? path
            : throw new ArgumentException($"Blob key '{key}' points outside the storage root.", nameof(key));
    }
}
