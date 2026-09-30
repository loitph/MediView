using MediView.BuildingBlocks.Domain;

namespace MediView.Identity.Domain.Doctors;

public sealed class Doctor : AggregateRoot<Guid>
{
    private readonly List<DoctorSchedule> _schedules = [];

    private Doctor()
    {
    }

    public Guid UserId { get; private set; }
    public string LicenseNumber { get; private set; } = null!;
    public string Specialty { get; private set; } = null!;
    public IReadOnlyCollection<DoctorSchedule> Schedules => _schedules.AsReadOnly();

    public static Doctor Create(Guid userId, string licenseNumber, string specialty) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        LicenseNumber = licenseNumber.Trim(),
        Specialty = specialty.Trim(),
    };

    public void AddSchedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, int slotMinutes)
    {
        if (slotMinutes <= 0)
        {
            throw new DomainException("A slot must last at least one minute.");
        }

        if (startTime >= endTime)
        {
            throw new DomainException($"The {dayOfWeek} shift must start before it ends.");
        }

        if (_schedules.Any(schedule => schedule.DayOfWeek == dayOfWeek))
        {
            throw new DomainException($"{dayOfWeek} already has a shift.");
        }

        _schedules.Add(DoctorSchedule.Create(Id, dayOfWeek, startTime, endTime, slotMinutes));
    }

    public IEnumerable<ScheduledSlot> SlotsBetween(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var schedule in _schedules.Where(schedule => schedule.DayOfWeek == date.DayOfWeek))
            {
                foreach (var slot in schedule.SlotsOn(date))
                {
                    yield return slot;
                }
            }
        }
    }
}
