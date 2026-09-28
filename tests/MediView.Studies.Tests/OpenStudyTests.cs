using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;
using MediView.Studies.Infrastructure.Locking;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class OpenStudyTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly LockOwner AssignedDoctor = new(Guid.NewGuid(), "Dr Assigned");
    private static readonly LockOwner OtherDoctor = new(Guid.NewGuid(), "Dr Other");

    private readonly InMemoryStudyRepository _studies = new();
    private readonly RedisStudyLock _lock;
    private readonly StudiesApplication _application;
    private readonly Study _study = Study.Book(
        Guid.NewGuid(), "Test Patient", "MRN-0001", AssignedDoctor.DoctorId, AssignedDoctor.DoctorName, Now.AddDays(1), StudyPriority.Routine, Now);

    public OpenStudyTests(RedisFixture redis)
    {
        _lock = new RedisStudyLock(redis.Connection);
        _studies.Add(_study);
        _application = new StudiesApplication(_lock, _studies);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _lock.ForceRelease(_study.Id);
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task AssignedDoctorOpensImportedStudyAndHoldsTheLock()
    {
        _study.MarkImagesImported(Now);

        var result = await Open(_study.Id, AssignedDoctor);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.InProgress, _study.Status);
        Assert.Equal(1, _studies.SaveCount);
        Assert.Equal(new LockResult(false, AssignedDoctor), await _lock.TryAcquire(_study.Id, OtherDoctor));
    }

    [Fact]
    public async Task SecondDoctorGetsConflictNamingTheFirstDoctor()
    {
        _study.MarkImagesImported(Now);
        await Open(_study.Id, AssignedDoctor);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => Open(_study.Id, OtherDoctor));

        Assert.Contains(AssignedDoctor.DoctorName, conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReopeningByTheReadingDoctorChangesNothing()
    {
        _study.MarkImagesImported(Now);
        await Open(_study.Id, AssignedDoctor);

        var result = await Open(_study.Id, AssignedDoctor);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.InProgress, _study.Status);
        Assert.Equal(2, _study.History.Count);
        Assert.Equal(1, _studies.SaveCount);
    }

    [Fact]
    public async Task OpeningStudyWithNoImagesLeavesNoLockBehind()
    {
        await Assert.ThrowsAsync<DomainException>(() => Open(_study.Id, AssignedDoctor));

        Assert.Equal(StudyStatus.ToDo, _study.Status);
        Assert.Equal(0, _studies.SaveCount);
        Assert.True((await _lock.TryAcquire(_study.Id, OtherDoctor)).Acquired);
    }

    [Fact]
    public async Task OpeningStudyByUnassignedDoctorLeavesNoLockBehind()
    {
        _study.MarkImagesImported(Now);

        await Assert.ThrowsAsync<DomainException>(() => Open(_study.Id, OtherDoctor));

        Assert.True((await _lock.TryAcquire(_study.Id, AssignedDoctor)).Acquired);
    }

    [Fact]
    public async Task OpeningUnknownStudyFailsAndLeavesNoLockBehind()
    {
        var unknownId = Guid.NewGuid();

        var result = await Open(unknownId, AssignedDoctor);

        Assert.Equal(StudyErrors.NotFound(unknownId), result.Error);
        Assert.True((await _lock.TryAcquire(unknownId, OtherDoctor)).Acquired);
        await _lock.ForceRelease(unknownId);
    }

    private Task<Result> Open(Guid studyId, LockOwner doctor) =>
        _application.Send(new OpenStudyCommand(studyId, doctor));
}
