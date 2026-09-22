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
}
