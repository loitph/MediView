using MediView.BuildingBlocks.Domain;

namespace MediView.Studies.Domain.Studies;

public sealed class Study : AggregateRoot<Guid>
{
    private readonly List<StudyStatusHistory> _history = [];

    private Study()
    {
    }

    public string StudyNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PatientMrn { get; private set; } = null!;
    public Guid DoctorId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public DateTimeOffset ScheduledStart { get; private set; }
    public StudyPriority Priority { get; private set; }
    public StudyStatus Status { get; private set; }
    public DateTimeOffset? ImagesImportedAt { get; private set; }
    public IReadOnlyCollection<StudyStatusHistory> History => _history.AsReadOnly();
}
