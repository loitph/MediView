using Xunit;

namespace MediView.BuildingBlocks.Domain.Tests;

internal sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id) => Id = id;
}

public sealed class AggregateRootTests
{
    [Fact]
    public void EntitiesWithSameIdAreEqualButTransientOnesAreNot()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestAggregate(id), new TestAggregate(id));
        Assert.NotEqual(new TestAggregate(Guid.Empty), new TestAggregate(Guid.Empty));
    }
}
