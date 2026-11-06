using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudyStatus
{
    ToDo,
    InProgress,
    Done,
    ReDiagnosis,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudyActor
{
    Patient,
    Doctor,
    Admin,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Draft,
    Finalized,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportDecision
{
    Complete,
    ReDiagnosis,
}

public record LoginResponse(string Token);

public record RegisterRequest(string Email, string Password, string FullName);

public record DoctorSummary(Guid Id, string FullName, string Specialty);

public record CreateDoctorRequest(
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    string Specialty,
    List<ShiftRequest> Shifts);

public record ShiftRequest(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End, int SlotMinutes);

public record AvailableSlot(DateTimeOffset Start, DateTimeOffset End);

public record StudySummary(
    Guid Id,
    string StudyNumber,
    string PatientName,
    string PatientMrn,
    string DoctorName,
    DateTimeOffset ScheduledStart,
    StudyStatus Status,
    bool ImagesImported);

public record StatusChange(StudyStatus? From, StudyStatus To, DateTimeOffset ChangedAt, StudyActor ChangedBy, string? Reason);

public record StudyDetail(StudySummary Study, List<StatusChange> History);

public record InstanceSummary(Guid Id, string SeriesInstanceUid);

public record ImportResult(int Imported, int AlreadyPresent);

public record ReportView(
    Guid Id,
    string DoctorName,
    ReportStatus Status,
    string? Findings,
    string? Impression,
    ReportDecision? Decision,
    DateTimeOffset? FinalizedAt,
    List<MedicationView> Medications);

public record MedicationView(Guid Id, string DrugName, string Dosage, string Frequency, string Duration);

public record MedicationRequest(string DrugName, string Dosage, string Frequency, string Duration);
