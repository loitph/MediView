using MediView.BuildingBlocks.Application;
using MediView.Identity.Domain.Doctors;

namespace MediView.Identity.Application.Doctors;

public sealed record AvailableSlot(DateTimeOffset Start, DateTimeOffset End);

public sealed record GetDoctorAvailabilityQuery(Guid DoctorId, DateOnly From, DateOnly To)
    : IQuery<Result<IReadOnlyList<AvailableSlot>>>
{
    public const int MaxDays = 31;

    public bool HasValidRange => From <= To && To.DayNumber - From.DayNumber < MaxDays;
}

internal sealed class GetDoctorAvailabilityQueryHandler(IDoctorRepository doctors, TimeProvider timeProvider)
    : IQueryHandler<GetDoctorAvailabilityQuery, Result<IReadOnlyList<AvailableSlot>>>
{
    public async Task<Result<IReadOnlyList<AvailableSlot>>> Handle(
        GetDoctorAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.HasValidRange)
        {
            return Result.Failure<IReadOnlyList<AvailableSlot>>(DoctorErrors.InvalidRange);
        }

        var doctor = await doctors.FindWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure<IReadOnlyList<AvailableSlot>>(DoctorErrors.NotFound(request.DoctorId));
        }

        var now = timeProvider.GetUtcNow();
        var clinicTimeZone = timeProvider.LocalTimeZone;

        return doctor.SlotsBetween(request.From, request.To)
            .Select(slot => new AvailableSlot(
                InClinicTime(slot.Date, slot.Start, clinicTimeZone),
                InClinicTime(slot.Date, slot.End, clinicTimeZone)))
            .Where(slot => slot.Start > now)
            .ToList();
    }

    private static DateTimeOffset InClinicTime(DateOnly date, TimeOnly time, TimeZoneInfo clinicTimeZone)
    {
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, clinicTimeZone.GetUtcOffset(local));
    }
}
