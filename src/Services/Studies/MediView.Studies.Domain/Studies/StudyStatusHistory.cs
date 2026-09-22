namespace MediView.Studies.Domain.Studies;

public sealed class StudyStatusHistory
{
    private StudyStatusHistory()
    {
    }

    public StudyStatus? FromStatus { get; private set; }
    public StudyStatus ToStatus { get; private set; }
    public Guid ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
}
