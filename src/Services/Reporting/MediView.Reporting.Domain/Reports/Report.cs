using MediView.BuildingBlocks.Domain;

namespace MediView.Reporting.Domain.Reports;

public sealed class Report : AggregateRoot<Guid>
{
    private readonly List<ReportMedication> _medications = [];

    private Report()
    {
    }

    public Guid StudyId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public ReportStatus Status { get; private set; }
    public string? Findings { get; private set; }
    public string? Impression { get; private set; }
    public ReportDecision? Decision { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public IReadOnlyCollection<ReportMedication> Medications => _medications.AsReadOnly();

    public static Report Draft(Guid studyId, Guid doctorId, string doctorName) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            StudyId = studyId,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Status = ReportStatus.Draft,
        };

    public void UpdateText(string? findings, string? impression)
    {
        EnsureDraft();
        Findings = NullIfBlank(findings);
        Impression = NullIfBlank(impression);
    }

    public ReportMedication AddMedication(string drugName, string dosage, string frequency, string duration)
    {
        EnsureDraft();
        var medication = ReportMedication.Prescribe(drugName, dosage, frequency, duration);
        _medications.Add(medication);
        return medication;
    }

    public void RemoveMedication(Guid medicationId)
    {
        EnsureDraft();
        var medication = _medications.Find(candidate => candidate.Id == medicationId)
            ?? throw new DomainException($"Medication {medicationId} is not on report {Id}.");
        _medications.Remove(medication);
    }

    public void Finalize(ReportDecision decision, DateTimeOffset now)
    {
        EnsureDraft();

        if (Findings is null || Impression is null)
        {
            throw new DomainException($"Report {Id} needs findings and an impression before it can be finalized.");
        }

        Decision = decision;
        FinalizedAt = now;
        Status = ReportStatus.Finalized;
    }

    private void EnsureDraft()
    {
        if (Status != ReportStatus.Draft)
        {
            throw new DomainException($"Report {Id} is finalized and cannot change.");
        }
    }

    private static string? NullIfBlank(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
