using MediView.Studies.Application.Locking;
using MediView.Studies.Infrastructure.Locking;
using StackExchange.Redis;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class RedisStudyLockTests(RedisFixture redis) : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private static readonly LockOwner First = new(Guid.NewGuid(), "Dr First");
    private static readonly LockOwner Second = new(Guid.NewGuid(), "Dr Second");

    private readonly Guid _studyId = Guid.NewGuid();
    private readonly RedisStudyLock _lock = new(redis.Connection);

    private IDatabase Database => redis.Connection.GetDatabase();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _lock.ForceRelease(_studyId);

    [Fact]
    public async Task AcquireOnFreeStudySucceedsAndStoresOwnerWithTtl()
    {
        var result = await _lock.TryAcquire(_studyId, First);

        Assert.Equal(new LockResult(true, First), result);
        Assert.Equal($"{First.DoctorId}|{First.DoctorName}", await Database.StringGetAsync(RedisStudyLock.KeyFor(_studyId)));
        Assert.InRange(await RemainingTtl(), TimeSpan.FromMinutes(14), RedisStudyLock.Ttl);
    }

    [Fact]
    public async Task SecondDoctorIsRefusedAndToldTheOwner()
    {
        await _lock.TryAcquire(_studyId, First);

        var result = await _lock.TryAcquire(_studyId, Second);

        Assert.Equal(new LockResult(false, First), result);
    }

    [Fact]
    public async Task SameDoctorReacquiresAndRenewsTheTtl()
    {
        await _lock.TryAcquire(_studyId, First);
        await Database.KeyExpireAsync(RedisStudyLock.KeyFor(_studyId), TimeSpan.FromMinutes(1));

        var result = await _lock.TryAcquire(_studyId, First);

        Assert.True(result.Acquired);
        Assert.True(await RemainingTtl() > TimeSpan.FromMinutes(14));
    }

    [Fact]
    public async Task RenewByOwnerSucceedsAndByAnotherDoctorFails()
    {
        await _lock.TryAcquire(_studyId, First);

        Assert.True(await _lock.Renew(_studyId, First.DoctorId));
        Assert.False(await _lock.Renew(_studyId, Second.DoctorId));
    }

    [Fact]
    public async Task ReleaseByDifferentDoctorIsNoOp()
    {
        await _lock.TryAcquire(_studyId, First);

        Assert.False(await _lock.Release(_studyId, Second.DoctorId));

        Assert.Equal(new LockResult(false, First), await _lock.TryAcquire(_studyId, Second));
    }

    [Fact]
    public async Task ReleaseByOwnerFreesTheStudy()
    {
        await _lock.TryAcquire(_studyId, First);

        Assert.True(await _lock.Release(_studyId, First.DoctorId));

        Assert.True((await _lock.TryAcquire(_studyId, Second)).Acquired);
    }

    [Fact]
    public async Task ExpiredLockIsAcquirableByAnotherDoctor()
    {
        await _lock.TryAcquire(_studyId, First);
        await Database.KeyExpireAsync(RedisStudyLock.KeyFor(_studyId), TimeSpan.FromMilliseconds(1));
        await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);

        var result = await _lock.TryAcquire(_studyId, Second);

        Assert.Equal(new LockResult(true, Second), result);
    }

    [Fact]
    public async Task ForceReleaseRemovesAnyOwner()
    {
        await _lock.TryAcquire(_studyId, First);

        await _lock.ForceRelease(_studyId);

        Assert.True((await _lock.TryAcquire(_studyId, Second)).Acquired);
    }

    private async Task<TimeSpan> RemainingTtl() =>
        await Database.KeyTimeToLiveAsync(RedisStudyLock.KeyFor(_studyId)) ?? TimeSpan.Zero;
}
