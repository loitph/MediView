using Xunit;

namespace MediView.BuildingBlocks.Domain.Tests;

internal sealed record SomethingHappened(Guid Id) : DomainEvent;

internal sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id) => Id = id;

    public void DoSomething() => Raise(new SomethingHappened(Id));
}

public sealed class AggregateRootTests
{
    [Fact]
    public void RaiseRecordsEventAndClearEmptiesIt()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DoSomething();

        var raised = Assert.Single(aggregate.DomainEvents);
        Assert.IsType<SomethingHappened>(raised);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void EntitiesWithSameIdAreEqualButTransientOnesAreNot()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestAggregate(id), new TestAggregate(id));
        Assert.NotEqual(new TestAggregate(Guid.Empty), new TestAggregate(Guid.Empty));
    }
}
