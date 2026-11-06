using MediView.Shared;

namespace MediView.Reporting.Api.Entities;

public enum ReportStatus
{
    Draft,
    Finalized,
}

public enum ReportDecision
{
    Complete,
    ReDiagnosis,
}

public class Report
{
    public Guid Id { get; set; }
    public Guid StudyId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = null!;
    public ReportStatus Status { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public ReportDecision? Decision { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public List<ReportMedication> Medications { get; set; } = [];

    public void UpdateText(string? findings, string? impression)
    {
        EnsureDraft();
        Findings = findings?.Trim();
        Impression = impression?.Trim();
    }

    public ReportMedication AddMedication(string drugName, string dosage, string frequency, string duration)
    {
        EnsureDraft();
        var medication = new ReportMedication
        {
            Id = Guid.CreateVersion7(),
            DrugName = drugName.Trim(),
            Dosage = dosage.Trim(),
            Frequency = frequency.Trim(),
            Duration = duration.Trim(),
        };
        Medications.Add(medication);
        return medication;
    }

    public void RemoveMedication(Guid medicationId)
    {
        EnsureDraft();
        Medications.RemoveAll(m => m.Id == medicationId);
    }

    public void Finalize(ReportDecision decision)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(Findings) || string.IsNullOrWhiteSpace(Impression))
        {
            throw new DomainException("Write the findings and the impression before finalizing.");
        }

        Decision = decision;
        FinalizedAt = DateTimeOffset.UtcNow;
        Status = ReportStatus.Finalized;
    }

    private void EnsureDraft()
    {
        if (Status != ReportStatus.Draft)
        {
            throw new DomainException("This report is finalized and cannot change.");
        }
    }
}

public class ReportMedication
{
    public Guid Id { get; set; }
    public string DrugName { get; set; } = null!;
    public string Dosage { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Duration { get; set; } = null!;
}
