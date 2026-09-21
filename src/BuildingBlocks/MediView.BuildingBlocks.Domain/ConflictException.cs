namespace MediView.BuildingBlocks.Domain;

public sealed class ConflictException(string message) : DomainException(message);
