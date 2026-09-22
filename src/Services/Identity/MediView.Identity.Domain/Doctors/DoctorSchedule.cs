using MediView.BuildingBlocks.Domain;

namespace MediView.Identity.Domain.Doctors;

public sealed class DoctorSchedule : Entity<Guid>
{
    private DoctorSchedule()
    {
    }

    public Guid DoctorId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotMinutes { get; private set; }

    internal static DoctorSchedule Create(
        Guid doctorId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotMinutes) => new()
    {
        Id = Guid.CreateVersion7(),
        DoctorId = doctorId,
        DayOfWeek = dayOfWeek,
        StartTime = startTime,
        EndTime = endTime,
        SlotMinutes = slotMinutes,
    };
}
