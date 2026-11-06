namespace MediView.Identity.Api.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Mrn { get; set; } = null!;
}
