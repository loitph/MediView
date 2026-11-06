using MediView.Shared;

namespace MediView.Studies.Api.Entities;

public enum StudyStatus
{
    ToDo,
    InProgress,
    Done,
    ReDiagnosis,
}

public class Study
{
    private static readonly List<(StudyStatus From, StudyStatus To)> AllowedMoves =
    [
        (StudyStatus.ToDo, StudyStatus.InProgress),
        (StudyStatus.InProgress, StudyStatus.Done),
        (StudyStatus.InProgress, StudyStatus.ReDiagnosis),
        (StudyStatus.ReDiagnosis, StudyStatus.InProgress),
    ];

    public Guid Id { get; set; }
    public string StudyNumber { get; set; } = null!;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = null!;
    public string PatientMrn { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = null!;
    public DateTimeOffset ScheduledStart { get; set; }
    public StudyStatus Status { get; set; }
    public DateTimeOffset? ImagesImportedAt { get; set; }
    public List<StudyStatusHistory> History { get; set; } = [];

    public void StartRead(Guid doctorId)
    {
        if (ImagesImportedAt is null)
        {
            throw new DomainException($"Study {StudyNumber} cannot be read before its images are imported.");
        }

        if (doctorId != DoctorId)
        {
            throw new DomainException($"Study {StudyNumber} is booked with {DoctorName}.");
        }

        MoveTo(StudyStatus.InProgress, doctorId);
    }

    public void Complete() => MoveTo(StudyStatus.Done, DoctorId);

    public void RequestReDiagnosis() => MoveTo(StudyStatus.ReDiagnosis, DoctorId);

    public void Override(StudyStatus to, Guid adminId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("An override needs a reason.");
        }

        if (to == Status)
        {
            throw new DomainException($"Study {StudyNumber} is already {Status}.");
        }

        AddHistory(to, adminId, reason.Trim());
    }

    private void MoveTo(StudyStatus to, Guid changedBy)
    {
        if (!AllowedMoves.Contains((Status, to)))
        {
            throw new DomainException($"Study {StudyNumber} cannot move from {Status} to {to}.");
        }

        AddHistory(to, changedBy, null);
    }

    private void AddHistory(StudyStatus to, Guid changedBy, string? reason)
    {
        History.Add(new StudyStatusHistory
        {
            FromStatus = Status,
            ToStatus = to,
            ChangedBy = changedBy,
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow,
        });
        Status = to;
    }
}

public class StudyStatusHistory
{
    public StudyStatus? FromStatus { get; set; }
    public StudyStatus ToStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
