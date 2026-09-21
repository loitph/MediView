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
        LicenseNumber = licenseNumber,
        Specialty = specialty,
    };
}
