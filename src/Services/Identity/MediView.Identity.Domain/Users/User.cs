using MediView.BuildingBlocks.Domain;

namespace MediView.Identity.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    private User()
    {
    }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public Role Role { get; private set; }

    public static User Create(string email, string fullName, Role role, Func<User, string> hashPassword)
    {
        ArgumentNullException.ThrowIfNull(hashPassword);

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            Role = role,
        };
        user.PasswordHash = hashPassword(user);

        return user;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
