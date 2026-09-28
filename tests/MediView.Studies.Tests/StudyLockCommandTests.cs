using MediView.BuildingBlocks.Domain;
using MediView.Studies.Application.Locking;
using MediView.Studies.Infrastructure.Locking;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class StudyLockCommandTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private static readonly LockOwner Owner = new(Guid.NewGuid(), "Dr Owner");
    private static readonly LockOwner Other = new(Guid.NewGuid(), "Dr Other");

    private readonly Guid _studyId = Guid.NewGuid();
    private readonly RedisStudyLock _lock;
    private readonly StudiesApplication _application;

    public StudyLockCommandTests(RedisFixture redis)
    {
        _lock = new RedisStudyLock(redis.Connection);
        _application = new StudiesApplication(_lock, new InMemoryStudyRepository());
    }

    public async ValueTask InitializeAsync() => await _lock.TryAcquire(_studyId, Owner);

    public async ValueTask DisposeAsync()
    {
        await _lock.ForceRelease(_studyId);
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task HeartbeatFromOwnerSucceeds()
    {
        var result = await _application.Send(new RenewStudyLockCommand(_studyId, Owner.DoctorId));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HeartbeatFromNonOwnerIsConflict()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            _application.Send(new RenewStudyLockCommand(_studyId, Other.DoctorId)));
    }

    [Fact]
    public async Task HeartbeatAfterTheLockEndedIsConflict()
    {
        await _lock.ForceRelease(_studyId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _application.Send(new RenewStudyLockCommand(_studyId, Owner.DoctorId)));
    }

    [Fact]
    public async Task ReleaseFromOwnerFreesTheStudy()
    {
        await _application.Send(new ReleaseStudyLockCommand(_studyId, Owner.DoctorId));

        Assert.Null(await _application.Send(new GetStudyLockOwnerQuery(_studyId)));
    }

    [Fact]
    public async Task ReleaseFromNonOwnerIsConflictAndKeepsTheLock()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            _application.Send(new ReleaseStudyLockCommand(_studyId, Other.DoctorId)));

        Assert.Equal(Owner, await _application.Send(new GetStudyLockOwnerQuery(_studyId)));
    }

    [Fact]
    public async Task GetOwnerReturnsNoneForAnUnlockedStudy()
    {
        Assert.Null(await _application.Send(new GetStudyLockOwnerQuery(Guid.NewGuid())));
    }
}
