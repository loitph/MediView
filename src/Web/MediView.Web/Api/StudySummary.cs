namespace MediView.Web.Api;

public sealed record StudySummary(
    Guid Id,
    string StudyNumber,
    string PatientName,
    string PatientMrn,
    string DoctorName,
    DateTimeOffset ScheduledStart,
    StudyPriority Priority,
    StudyStatus Status,
    bool ImagesImported);
