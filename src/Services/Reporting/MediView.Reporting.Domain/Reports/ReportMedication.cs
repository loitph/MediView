using MediView.BuildingBlocks.Domain;

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

    internal static ReportMedication Prescribe(string drugName, string dosage, string frequency, string duration) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            DrugName = Required(drugName, nameof(DrugName)),
            Dosage = Required(dosage, nameof(Dosage)),
            Frequency = Required(frequency, nameof(Frequency)),
            Duration = Required(duration, nameof(Duration)),
        };

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainException($"A medication requires {field}.")
            : value.Trim();
}
