using System.Globalization;
using MediView.Studies.Application.Locking;
using StackExchange.Redis;

namespace MediView.Studies.Infrastructure.Locking;

public sealed class RedisStudyLock(IConnectionMultiplexer redis) : IStudyLock
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    private const char Separator = '|';

    private const string RenewIfOwnerScript = """
        local owner = redis.call('GET', KEYS[1])
        if owner and string.sub(owner, 1, #ARGV[1] + 1) == ARGV[1] .. '|' then
            return redis.call('PEXPIRE', KEYS[1], ARGV[2])
        end
        return 0
        """;

    private const string ReleaseIfOwnerScript = """
        local owner = redis.call('GET', KEYS[1])
        if owner and string.sub(owner, 1, #ARGV[1] + 1) == ARGV[1] .. '|' then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private IDatabase Database => redis.GetDatabase();

    public static RedisKey KeyFor(Guid studyId) => $"study-lock:{studyId}";

    public async Task<LockResult> TryAcquire(Guid studyId, LockOwner owner)
    {
        var key = KeyFor(studyId);

        while (true)
        {
            if (await Database.StringSetAsync(key, Serialize(owner), Ttl, When.NotExists))
            {
                return new LockResult(true, owner);
            }

            if (await Renew(studyId, owner.DoctorId))
            {
                return new LockResult(true, owner);
            }

            var stored = await Database.StringGetAsync(key);
            if (stored.HasValue)
            {
                return new LockResult(false, Deserialize(stored.ToString()));
            }
        }
    }

    public async Task<LockOwner?> GetOwner(Guid studyId)
    {
        var stored = await Database.StringGetAsync(KeyFor(studyId));
        return stored.HasValue ? Deserialize(stored.ToString()) : null;
    }

    public Task<bool> Renew(Guid studyId, Guid doctorId) =>
        RunIfOwner(RenewIfOwnerScript, studyId, doctorId, (long)Ttl.TotalMilliseconds);

    public Task<bool> Release(Guid studyId, Guid doctorId) =>
        RunIfOwner(ReleaseIfOwnerScript, studyId, doctorId);

    public Task ForceRelease(Guid studyId) => Database.KeyDeleteAsync(KeyFor(studyId));

    private async Task<bool> RunIfOwner(string script, Guid studyId, Guid doctorId, params RedisValue[] extraArguments)
    {
        var result = await Database.ScriptEvaluateAsync(script, [KeyFor(studyId)], [doctorId.ToString(), .. extraArguments]);
        return (long)result == 1;
    }

    private static string Serialize(LockOwner owner) => $"{owner.DoctorId}{Separator}{owner.DoctorName}";

    private static LockOwner Deserialize(string value)
    {
        var separatorIndex = value.IndexOf(Separator, StringComparison.Ordinal);
        return new LockOwner(
            Guid.Parse(value.AsSpan(0, separatorIndex), CultureInfo.InvariantCulture),
            value[(separatorIndex + 1)..]);
    }
}
