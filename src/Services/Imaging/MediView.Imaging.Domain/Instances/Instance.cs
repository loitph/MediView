using MediView.BuildingBlocks.Domain;

namespace MediView.Imaging.Domain.Instances;

public sealed class Instance : AggregateRoot<Guid>
{
    private Instance()
    {
    }

    public Guid StudyId { get; private set; }
    public string SeriesInstanceUid { get; private set; } = null!;
    public string SopInstanceUid { get; private set; } = null!;
    public int InstanceNumber { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public long SizeBytes { get; private set; }

    public static Instance Import(
        Guid studyId,
        string seriesInstanceUid,
        string sopInstanceUid,
        int instanceNumber,
        string storagePath,
        long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(seriesInstanceUid) || string.IsNullOrWhiteSpace(sopInstanceUid))
        {
            throw new DomainException("An instance needs both a series and a SOP instance UID.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException($"Instance {sopInstanceUid} has no bytes.");
        }

        return new Instance
        {
            Id = Guid.CreateVersion7(),
            StudyId = studyId,
            SeriesInstanceUid = seriesInstanceUid,
            SopInstanceUid = sopInstanceUid,
            InstanceNumber = instanceNumber,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
        };
    }
}
