using MediView.Studies.Application.Locking;
using MediView.Studies.Infrastructure.Locking;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class StudyLockConcurrencyTests(RedisFixture redis) : IClassFixture<RedisFixture>
{
    private const int Doctors = 20;
    private const int Rounds = 25;

    private readonly RedisStudyLock _lock = new(redis.Connection);

    [Fact]
    public async Task ExactlyOneOfTwentyConcurrentDoctorsWinsEveryRound()
    {
        for (var round = 0; round < Rounds; round++)
        {
            await AssertSingleWinner(Guid.NewGuid());
        }
    }

    private async Task AssertSingleWinner(Guid studyId)
    {
        var doctors = Enumerable.Range(1, Doctors)
            .Select(number => new LockOwner(Guid.NewGuid(), $"Dr {number}"))
            .ToList();

        try
        {
            var results = await Task.WhenAll(doctors.Select(doctor => Task.Run(() => _lock.TryAcquire(studyId, doctor))));

            var winner = Assert.Single(results, result => result.Acquired).CurrentOwner;
            Assert.All(results, result => Assert.Equal(winner, result.CurrentOwner));

            var loser = doctors.First(doctor => doctor != winner);
            Assert.False(await _lock.Release(studyId, loser.DoctorId));
            Assert.Equal(winner, await _lock.GetOwner(studyId));
        }
        finally
        {
            await _lock.ForceRelease(studyId);
        }
    }
}
