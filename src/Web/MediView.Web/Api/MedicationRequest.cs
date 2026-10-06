namespace MediView.Web.Api;

public sealed record MedicationRequest(string DrugName, string Dosage, string Frequency, string Duration);
