using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;
using MediView.Studies.Infrastructure.Locking;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class FinishReadTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly LockOwner Doctor = new(Guid.NewGuid(), "Dr. B");
    private static readonly LockOwner Colleague = new(Guid.NewGuid(), "Dr. C");
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly InMemoryStudyRepository _studies = new();
    private readonly RedisStudyLock _lock;
    private readonly StudiesApplication _application;
    private readonly Study _study = Study.Book(
        Guid.NewGuid(), "Patient A", "MRN-1042", Doctor.DoctorId, Doctor.DoctorName, Now.AddDays(1), StudyPriority.Routine, Now);

    public FinishReadTests(RedisFixture redis)
    {
        _lock = new RedisStudyLock(redis.Connection);
        _studies.Add(_study);
        _application = new StudiesApplication(_lock, _studies);
    }

    public async ValueTask InitializeAsync()
    {
        _study.MarkImagesImported(Now);
        await _application.Send(new OpenStudyCommand(_study.Id, Doctor));
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.ForceRelease(_study.Id);
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task CompletingTheReadMovesTheStudyToDoneAndFreesTheLock()
    {
        await FinishRead(ReadDecision.Complete);

        Assert.Equal(StudyStatus.Done, _study.Status);
        Assert.Null(await _lock.GetOwner(_study.Id));
    }

    [Fact]
    public async Task ReDiagnosisMovesTheStudyToReDiagnosis()
    {
        await FinishRead(ReadDecision.ReDiagnosis);

        Assert.Equal(StudyStatus.ReDiagnosis, _study.Status);
    }

    [Fact]
    public async Task FinishingTheReadTwiceLeavesTheStudyWhereTheFirstCallPutIt()
    {
        await FinishRead(ReadDecision.Complete);
        var historyBefore = _study.History.Count;

        var result = await FinishRead(ReadDecision.Complete);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Done, _study.Status);
        Assert.Equal(historyBefore, _study.History.Count);
    }

    [Fact]
    public async Task ForceReleaseFreesTheLockSoAColleagueCanOpenAndLeavesANote()
    {
        await _application.Send(new ForceReleaseStudyLockCommand(_study.Id, AdminId, "Dr. B is off sick."));

        Assert.True((await _lock.TryAcquire(_study.Id, Colleague)).Acquired);
        var note = _study.History.Last();
        Assert.Equal(note.FromStatus, note.ToStatus);
        Assert.Equal(AdminId, note.ChangedBy);
        Assert.Equal("Dr. B is off sick.", note.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ForceReleaseWithoutAReasonKeepsTheLock(string reason)
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _application.Send(new ForceReleaseStudyLockCommand(_study.Id, AdminId, reason)));

        Assert.Equal(Doctor, await _lock.GetOwner(_study.Id));
    }

    private Task<Result> FinishRead(ReadDecision decision) =>
        _application.Send(new FinishReadCommand(_study.Id, Doctor.DoctorId, decision));
}
