namespace MediView.Web.Api;

public sealed record CreateDoctorRequest(
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    string Specialty,
    IReadOnlyList<ShiftRequest> Shifts);

public sealed record ShiftRequest(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End, int SlotMinutes);
