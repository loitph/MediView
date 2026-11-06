namespace MediView.Identity.Api.Entities;

public class Doctor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public string Specialty { get; set; } = null!;
    public List<DoctorSchedule> Schedules { get; set; } = [];
}

public class DoctorSchedule
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}
