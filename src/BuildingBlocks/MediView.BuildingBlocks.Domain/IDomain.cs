namespace MediView.BuildingBlocks.Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}