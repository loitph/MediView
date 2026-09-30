using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Doctors;

public static class DoctorErrors
{
    public static readonly Error LicenseTaken = new("doctor.license_taken", "A doctor with this licence number already exists.");

    public static readonly Error InvalidRange = new(
        "doctor.invalid_range",
        $"'to' must not be before 'from', and the range may span at most {GetDoctorAvailabilityQuery.MaxDays} days.");

    public static Error NotFound(Guid doctorId) => new("doctor.not_found", $"Doctor {doctorId} does not exist.");
}
