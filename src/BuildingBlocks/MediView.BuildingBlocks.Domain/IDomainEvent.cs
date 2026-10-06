namespace MediView.BuildingBlocks.Domain;

public interface IDomainEvent
{
    public DateTimeOffset OccurredOn { get; }
}
