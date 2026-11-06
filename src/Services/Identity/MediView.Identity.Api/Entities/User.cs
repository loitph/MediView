namespace MediView.Identity.Api.Entities;

public enum Role
{
    Patient,
    Doctor,
    Admin,
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Role Role { get; set; }
}
