namespace MediView.Web.Api;

public sealed record ReportView(
    Guid Id,
    string DoctorName,
    ReportStatus Status,
    string? Findings,
    string? Impression,
    ReportDecision? Decision,
    DateTimeOffset? FinalizedAt,
    IReadOnlyList<MedicationView> Medications);
