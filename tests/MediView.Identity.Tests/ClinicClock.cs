namespace MediView.Identity.Tests;

internal sealed class ClinicClock(DateTimeOffset now, TimeZoneInfo clinicTimeZone) : TimeProvider
{
    public ClinicClock(DateTimeOffset now)
        : this(now, TimeZoneInfo.Utc)
    {
    }

    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => clinicTimeZone;
}
