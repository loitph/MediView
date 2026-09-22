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
}
