using MediView.BuildingBlocks.Application;
using MediView.Imaging.Domain.Instances;

namespace MediView.Imaging.Application.Instances;

public sealed record UploadedFile(string FileName, Func<Stream> OpenRead);

public sealed record ImportImagesCommand(Guid StudyId, IReadOnlyList<UploadedFile> Files) : ICommand<Result<ImportedImages>>;

public sealed record ImportedImages(int Imported, int AlreadyPresent);

internal sealed class ImportImagesCommandHandler(
    IStudiesClient studies,
    IDicomReader dicom,
    IBlobStore blobs,
    IInstanceRepository instances) : ICommandHandler<ImportImagesCommand, Result<ImportedImages>>
{
    public async Task<Result<ImportedImages>> Handle(ImportImagesCommand request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return Result.Failure<ImportedImages>(ImagingErrors.NoFiles);
        }

        if (!await studies.CanReadAsync(request.StudyId, cancellationToken))
        {
            return Result.Failure<ImportedImages>(ImagingErrors.StudyNotFound(request.StudyId));
        }

        var parsed = new List<(UploadedFile File, DicomHeader Header)>();
        foreach (var file in request.Files)
        {
            await using var content = file.OpenRead();
            if (await dicom.ReadHeaderAsync(content, cancellationToken) is not { } header)
            {
                return Result.Failure<ImportedImages>(ImagingErrors.NotDicom(file.FileName));
            }

            parsed.Add((file, header));
        }

        var present = await instances.SopInstanceUidsOfAsync(request.StudyId, cancellationToken);
        var fresh = parsed
            .Where(upload => !present.Contains(upload.Header.SopInstanceUid))
            .DistinctBy(upload => upload.Header.SopInstanceUid)
            .ToList();

        await StoreAsync(request.StudyId, fresh, cancellationToken);

        if (!await studies.TryMarkImagesImportedAsync(request.StudyId, cancellationToken))
        {
            return Result.Failure<ImportedImages>(ImagingErrors.StudyNotUpdated);
        }

        return new ImportedImages(fresh.Count, parsed.Count - fresh.Count);
    }

    private async Task StoreAsync(
        Guid studyId,
        IReadOnlyList<(UploadedFile File, DicomHeader Header)> uploads,
        CancellationToken cancellationToken)
    {
        var storedKeys = new List<string>();

        try
        {
            foreach (var (file, header) in uploads)
            {
                var key = BlobKey(studyId);
                await using var content = file.OpenRead();
                var sizeBytes = await blobs.SaveAsync(key, content, cancellationToken);
                storedKeys.Add(key);

                instances.Add(Instance.Import(
                    studyId,
                    header.SeriesInstanceUid,
                    header.SopInstanceUid,
                    header.InstanceNumber,
                    key,
                    sizeBytes));
            }

            await instances.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storedKeys.ForEach(blobs.Delete);
            throw;
        }
    }

    private static string BlobKey(Guid studyId) => $"{studyId}/{Guid.CreateVersion7():N}.dcm";
}
