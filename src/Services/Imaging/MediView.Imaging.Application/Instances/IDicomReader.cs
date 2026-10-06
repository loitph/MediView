namespace MediView.Imaging.Application.Instances;

public interface IDicomReader
{
    public Task<DicomHeader?> ReadHeaderAsync(Stream content, CancellationToken cancellationToken);
}

public sealed record DicomHeader(string SeriesInstanceUid, string SopInstanceUid, int InstanceNumber);
