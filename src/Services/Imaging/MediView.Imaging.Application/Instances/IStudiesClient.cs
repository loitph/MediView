namespace MediView.Imaging.Application.Instances;

public interface IStudiesClient
{
    public Task<bool> CanReadAsync(Guid studyId, CancellationToken cancellationToken);

    public Task<bool> TryMarkImagesImportedAsync(Guid studyId, CancellationToken cancellationToken);
}
