using MediView.Identity.Application.Auth;
using MediView.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = User.NormalizeEmail(email);
        return db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = User.NormalizeEmail(email);
        return db.Users.AnyAsync(user => user.Email == normalized, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);
}
