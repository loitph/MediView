namespace MediView.Web.Api;

public sealed record MedicationView(Guid Id, string DrugName, string Dosage, string Frequency, string Duration);
