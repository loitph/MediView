using MediView.Identity.Domain.Doctors;

namespace MediView.Identity.Application.Doctors;

public interface IDoctorRepository
{
    public Task<Guid?> FindIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    public Task<Doctor?> FindWithSchedulesAsync(Guid id, CancellationToken cancellationToken);

    public Task<DoctorSummary?> FindSummaryAsync(Guid id, CancellationToken cancellationToken);

    public Task<IReadOnlyList<DoctorSummary>> ListAsync(CancellationToken cancellationToken);

    public Task<bool> LicenseExistsAsync(string licenseNumber, CancellationToken cancellationToken);

    public void Add(Doctor doctor);
}
