using MediView.Identity.Domain.Users;

namespace MediView.Identity.Application.Auth;

public interface IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    public void Add(User user);
}
