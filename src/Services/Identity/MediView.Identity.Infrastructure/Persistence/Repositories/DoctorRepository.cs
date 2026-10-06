using MediView.Identity.Application.Doctors;
using MediView.Identity.Domain.Doctors;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Infrastructure.Persistence.Repositories;

internal sealed class DoctorRepository(IdentityDbContext db) : IDoctorRepository
{
    public Task<Guid?> FindIdByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Doctors
            .Where(doctor => doctor.UserId == userId)
            .Select(doctor => (Guid?)doctor.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Doctor?> FindWithSchedulesAsync(Guid id, CancellationToken cancellationToken) =>
        db.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.Schedules)
            .SingleOrDefaultAsync(doctor => doctor.Id == id, cancellationToken);

    public Task<DoctorSummary?> FindSummaryAsync(Guid id, CancellationToken cancellationToken) =>
        Summaries(db.Doctors.Where(doctor => doctor.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DoctorSummary>> ListAsync(CancellationToken cancellationToken) =>
        await Summaries(db.Doctors).ToListAsync(cancellationToken);

    public Task<bool> LicenseExistsAsync(string licenseNumber, CancellationToken cancellationToken)
    {
        var trimmed = licenseNumber.Trim();
        return db.Doctors.AnyAsync(doctor => doctor.LicenseNumber == trimmed, cancellationToken);
    }

    public void Add(Doctor doctor) => db.Doctors.Add(doctor);

    private IQueryable<DoctorSummary> Summaries(IQueryable<Doctor> doctors) =>
        from doctor in doctors.AsNoTracking()
        join user in db.Users on doctor.UserId equals user.Id
        orderby user.FullName
        select new DoctorSummary(doctor.Id, user.FullName, doctor.Specialty);
}
