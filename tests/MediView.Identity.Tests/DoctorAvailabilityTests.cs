using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Identity.Application.Doctors;
using MediView.Identity.Domain.Doctors;
using Xunit;

namespace MediView.Identity.Tests;

public sealed class DoctorAvailabilityTests
{
    private static readonly DateOnly Monday = new(2026, 10, 5);
    private static readonly DateOnly Sunday = new(2026, 10, 11);
    private static readonly DateTimeOffset SundayBefore = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly DayOfWeek[] Weekdays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
    ];

    private readonly InMemoryIdentityStore _store = new();

    [Fact]
    public async Task WeekdayEightToFourInHalfHoursYieldsSixteenSlotsPerWeekdayAndNoneAtTheWeekend()
    {
        var doctor = AddWeekdayDoctor(new TimeOnly(8, 0), new TimeOnly(16, 0), 30);

        var slots = await Availability(doctor.Id, Monday, Sunday, new ClinicClock(SundayBefore));

        var perDay = slots.Value.GroupBy(slot => slot.Start.DayOfWeek).ToDictionary(day => day.Key, day => day.Count());
        Assert.Equal(Weekdays, perDay.Keys);
        Assert.All(perDay.Values, count => Assert.Equal(16, count));
        Assert.DoesNotContain(DayOfWeek.Sunday, perDay.Keys);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero), slots.Value[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 16, 0, 0, TimeSpan.Zero), slots.Value[15].End);
    }

    [Fact]
    public async Task SlotsThatHaveAlreadyStartedAreDropped()
    {
        var doctor = AddWeekdayDoctor(new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        var mondayMidMorning = new DateTimeOffset(2026, 10, 5, 10, 10, 0, TimeSpan.Zero);

        var slots = await Availability(doctor.Id, Monday, Monday, new ClinicClock(mondayMidMorning));

        Assert.Equal(11, slots.Value.Count);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 10, 30, 0, TimeSpan.Zero), slots.Value[0].Start);
    }

    [Fact]
    public async Task SlotsCarryTheClinicTimeZoneOffset()
    {
        var clinicOffset = TimeSpan.FromHours(7);
        var clinic = TimeZoneInfo.CreateCustomTimeZone("clinic", clinicOffset, "Clinic", "Clinic");
        var doctor = AddWeekdayDoctor(new TimeOnly(8, 0), new TimeOnly(9, 0), 30);

        var slots = await Availability(doctor.Id, Monday, Monday, new ClinicClock(SundayBefore, clinic));

        Assert.Equal(new DateTimeOffset(2026, 10, 5, 8, 0, 0, clinicOffset), slots.Value[0].Start);
    }

    [Fact]
    public void ShiftThatDoesNotDivideEvenlyDropsTheShortTail()
    {
        var doctor = Doctor.Create(Guid.NewGuid(), "LIC-1", "Radiology");
        doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 45), 30);

        var slots = doctor.SlotsBetween(Monday, Monday).ToList();

        Assert.Equal([new TimeOnly(8, 0), new TimeOnly(8, 30), new TimeOnly(9, 0)], slots.Select(slot => slot.Start));
    }

    [Fact]
    public void ShiftEndingJustBeforeMidnightDoesNotWrapAround()
    {
        var doctor = Doctor.Create(Guid.NewGuid(), "LIC-1", "Radiology");
        doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(23, 0), new TimeOnly(23, 59), 30);

        var slot = Assert.Single(doctor.SlotsBetween(Monday, Monday));

        Assert.Equal(new TimeOnly(23, 0), slot.Start);
    }

    [Fact]
    public async Task RangeEndingBeforeItStartsIsRejected()
    {
        var doctor = AddWeekdayDoctor(new TimeOnly(8, 0), new TimeOnly(16, 0), 30);

        var result = await Availability(doctor.Id, Sunday, Monday, new ClinicClock(SundayBefore));

        Assert.Equal(DoctorErrors.InvalidRange, result.Error);
    }

    [Fact]
    public async Task RangeLongerThanTheLimitIsRejected()
    {
        var doctor = AddWeekdayDoctor(new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        var clock = new ClinicClock(SundayBefore);

        var atLimit = await Availability(doctor.Id, Monday, Monday.AddDays(GetDoctorAvailabilityQuery.MaxDays - 1), clock);
        var overLimit = await Availability(doctor.Id, Monday, Monday.AddDays(GetDoctorAvailabilityQuery.MaxDays), clock);

        Assert.True(atLimit.IsSuccess);
        Assert.Equal(DoctorErrors.InvalidRange, overLimit.Error);
    }

    [Fact]
    public async Task UnknownDoctorIsNotFound()
    {
        var unknownId = Guid.NewGuid();

        var result = await Availability(unknownId, Monday, Sunday, new ClinicClock(SundayBefore));

        Assert.Equal(DoctorErrors.NotFound(unknownId), result.Error);
    }

    [Fact]
    public void SecondShiftOnTheSameDayIsRejected()
    {
        var doctor = Doctor.Create(Guid.NewGuid(), "LIC-1", "Radiology");
        doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0), 30);

        Assert.Throws<DomainException>(() => doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(16, 0), 30));
    }

    [Fact]
    public void ShiftThatEndsBeforeItStartsIsRejected()
    {
        var doctor = Doctor.Create(Guid.NewGuid(), "LIC-1", "Radiology");

        Assert.Throws<DomainException>(() => doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(16, 0), new TimeOnly(8, 0), 30));
    }

    private Doctor AddWeekdayDoctor(TimeOnly start, TimeOnly end, int slotMinutes)
    {
        var doctor = Doctor.Create(Guid.NewGuid(), "LIC-1", "Radiology");
        foreach (var day in Weekdays)
        {
            doctor.AddSchedule(day, start, end, slotMinutes);
        }

        _store.Add(doctor);
        return doctor;
    }

    private async Task<Result<IReadOnlyList<AvailableSlot>>> Availability(
        Guid doctorId,
        DateOnly from,
        DateOnly to,
        TimeProvider clock)
    {
        await using var application = new IdentityApplication(_store, clock);
        return await application.Send(new GetDoctorAvailabilityQuery(doctorId, from, to));
    }
}
