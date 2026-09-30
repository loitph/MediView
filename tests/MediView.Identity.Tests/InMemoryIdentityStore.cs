using MediView.Identity.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Application.Doctors;
using MediView.Identity.Application.Patients;
using MediView.Identity.Domain.Doctors;
using MediView.Identity.Domain.Patients;
using MediView.Identity.Domain.Users;

namespace MediView.Identity.Tests;

internal sealed class InMemoryIdentityStore : IUserRepository, IPatientRepository, IDoctorRepository, IUnitOfWork
{
    private readonly List<User> _users = [];
    private readonly List<Patient> _patients = [];
    private readonly List<Doctor> _doctors = [];

    public IReadOnlyList<User> Users => _users;
    public IReadOnlyList<Patient> Patients => _patients;
    public IReadOnlyList<Doctor> Doctors => _doctors;
    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.SingleOrDefault(user => user.Email == User.NormalizeEmail(email)));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Any(user => user.Email == User.NormalizeEmail(email)));

    public void Add(User user) => _users.Add(user);

    public Task<PatientSnapshot?> FindSnapshotAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(
            (from patient in _patients
             join user in _users on patient.UserId equals user.Id
             where user.Id == userId
             select new PatientSnapshot(user.Id, user.FullName, patient.Mrn)).SingleOrDefault());

    public void Add(Patient patient) => _patients.Add(patient);

    public Task<Guid?> FindIdByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_doctors.SingleOrDefault(doctor => doctor.UserId == userId)?.Id);

    public Task<Doctor?> FindWithSchedulesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_doctors.SingleOrDefault(doctor => doctor.Id == id));

    public Task<DoctorSummary?> FindSummaryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Summaries().SingleOrDefault(doctor => doctor.Id == id));

    public Task<IReadOnlyList<DoctorSummary>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DoctorSummary>>([.. Summaries()]);

    public Task<bool> LicenseExistsAsync(string licenseNumber, CancellationToken cancellationToken) =>
        Task.FromResult(_doctors.Any(doctor => doctor.LicenseNumber == licenseNumber.Trim()));

    public void Add(Doctor doctor) => _doctors.Add(doctor);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private IEnumerable<DoctorSummary> Summaries() =>
        from doctor in _doctors
        join user in _users on doctor.UserId equals user.Id
        orderby user.FullName
        select new DoctorSummary(doctor.Id, user.FullName, doctor.Specialty);
}
