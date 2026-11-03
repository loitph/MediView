using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Doctors;

public static class DoctorErrors
{
    public static readonly Error LicenseTaken = new("A doctor with this licence number already exists.");

    public static readonly Error InvalidRange = new(
        $"'to' must not be before 'from', and the range may span at most {GetDoctorAvailabilityQuery.MaxDays} days.");

    public static Error NotFound(Guid doctorId) => new($"Doctor {doctorId} does not exist.");
}
