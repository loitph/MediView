namespace MediView.Imaging.Api.Entities;

public class Instance
{
    public Guid Id { get; set; }
    public Guid StudyId { get; set; }
    public string SeriesInstanceUid { get; set; } = null!;
    public string SopInstanceUid { get; set; } = null!;
    public int InstanceNumber { get; set; }
    public string StoragePath { get; set; } = null!;
    public long SizeBytes { get; set; }
}
