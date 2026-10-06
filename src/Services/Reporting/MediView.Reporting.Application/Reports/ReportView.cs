using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

public sealed record ReportView(
    Guid Id,
    Guid StudyId,
    string DoctorName,
    ReportStatus Status,
    string? Findings,
    string? Impression,
    ReportDecision? Decision,
    DateTimeOffset? FinalizedAt,
    IReadOnlyList<MedicationView> Medications);

public sealed record MedicationView(Guid Id, string DrugName, string Dosage, string Frequency, string Duration);
