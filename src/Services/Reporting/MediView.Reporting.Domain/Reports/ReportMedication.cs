namespace MediView.Reporting.Domain.Reports;

public sealed class ReportMedication
{
    private ReportMedication()
    {
    }

    public Guid Id { get; private set; }
    public string DrugName { get; private set; } = null!;
    public string Dosage { get; private set; } = null!;
    public string Frequency { get; private set; } = null!;
    public string Duration { get; private set; } = null!;
}
