using MediView.Identity.Domain.Users;

namespace MediView.Identity.Application.Auth;

public interface IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
