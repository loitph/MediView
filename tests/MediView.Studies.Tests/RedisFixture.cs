using StackExchange.Redis;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class RedisFixture : IAsyncLifetime
{
    private const string LocalRedis = "localhost:6379";

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public async ValueTask InitializeAsync() =>
        Connection = await ConnectionMultiplexer.ConnectAsync(LocalRedis);

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
