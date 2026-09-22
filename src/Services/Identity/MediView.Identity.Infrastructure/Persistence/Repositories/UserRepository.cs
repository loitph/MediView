using MediView.Identity.Application.Auth;
using MediView.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
}
