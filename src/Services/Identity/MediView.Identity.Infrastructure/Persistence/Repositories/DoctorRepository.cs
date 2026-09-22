using MediView.Identity.Application.Auth;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Infrastructure.Persistence.Repositories;

internal sealed class DoctorRepository(IdentityDbContext db) : IDoctorRepository
{
    public Task<Guid?> FindIdByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Doctors
            .Where(doctor => doctor.UserId == userId)
            .Select(doctor => (Guid?)doctor.Id)
            .SingleOrDefaultAsync(cancellationToken);
}
