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

    public static User Create(string email, string passwordHash, string fullName, Role role) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = email,
        PasswordHash = passwordHash,
        FullName = fullName,
        Role = role,
    };
}
