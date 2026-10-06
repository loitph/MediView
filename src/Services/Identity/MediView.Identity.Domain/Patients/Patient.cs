using MediView.BuildingBlocks.Domain;

namespace MediView.Identity.Domain.Patients;

public sealed class Patient : AggregateRoot<Guid>
{
    private Patient()
    {
    }

    public Guid UserId { get; private set; }
    public string Mrn { get; private set; } = null!;

    public static Patient Create(Guid userId) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
    };
}
