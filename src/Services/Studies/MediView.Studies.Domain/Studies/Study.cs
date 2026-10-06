using MediView.BuildingBlocks.Domain;

namespace MediView.Studies.Domain.Studies;

public sealed class Study : AggregateRoot<Guid>
{
    public const int MaxReasonLength = 500;

    private static readonly HashSet<(StudyStatus From, StudyStatus To)> AllowedTransitions =
    [
        (StudyStatus.ToDo, StudyStatus.InProgress),
        (StudyStatus.InProgress, StudyStatus.Done),
        (StudyStatus.InProgress, StudyStatus.ReDiagnosis),
        (StudyStatus.ReDiagnosis, StudyStatus.InProgress),
    ];

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

    public static Study Book(
        Guid patientId,
        string patientName,
        string patientMrn,
        Guid doctorId,
        string doctorName,
        DateTimeOffset scheduledStart,
        StudyPriority priority,
        DateTimeOffset now)
    {
        var study = new Study
        {
            Id = Guid.CreateVersion7(now),
            PatientId = patientId,
            PatientName = patientName,
            PatientMrn = patientMrn,
            DoctorId = doctorId,
            DoctorName = doctorName,
            ScheduledStart = scheduledStart,
            Priority = priority,
            Status = StudyStatus.ToDo,
        };
        study._history.Add(StudyStatusHistory.Record(null, StudyStatus.ToDo, patientId, null, now));
        return study;
    }

    public void MarkImagesImported(DateTimeOffset now) => ImagesImportedAt ??= now;

    public void StartRead(Guid doctorId, DateTimeOffset now)
    {
        if (ImagesImportedAt is null)
        {
            throw new DomainException($"Study {Id} cannot be read before its images are imported.");
        }

        if (doctorId != DoctorId)
        {
            throw new DomainException($"Doctor {doctorId} is not assigned to study {Id}.");
        }

        TransitionTo(StudyStatus.InProgress, doctorId, now);
    }

    public void Complete(DateTimeOffset now) => TransitionTo(StudyStatus.Done, DoctorId, now);

    public void RequestReDiagnosis(DateTimeOffset now) => TransitionTo(StudyStatus.ReDiagnosis, DoctorId, now);

    public void AdminOverride(StudyStatus to, Guid adminId, string reason, DateTimeOffset now)
    {
        var justification = RequireReason(reason, "An override");

        if (to == Status)
        {
            throw new DomainException($"Study {Id} is already {Status}.");
        }

        Apply(to, adminId, justification, now);
    }

    public void NoteLockForceRelease(Guid adminId, string reason, DateTimeOffset now) =>
        _history.Add(StudyStatusHistory.Record(Status, Status, adminId, RequireReason(reason, "A forced lock release"), now));

    private string RequireReason(string? reason, string action)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException($"{action} of study {Id} requires a reason.");
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= MaxReasonLength
            ? trimmed
            : throw new DomainException($"A reason may be at most {MaxReasonLength} characters.");
    }

    private void TransitionTo(StudyStatus to, Guid changedBy, DateTimeOffset now)
    {
        if (!AllowedTransitions.Contains((Status, to)))
        {
            throw new DomainException($"Study {Id} cannot move from {Status} to {to}.");
        }

        Apply(to, changedBy, null, now);
    }

    private void Apply(StudyStatus to, Guid changedBy, string? reason, DateTimeOffset now)
    {
        _history.Add(StudyStatusHistory.Record(Status, to, changedBy, reason, now));
        Status = to;
    }
}
