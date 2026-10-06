using System.Globalization;

namespace MediView.Web.Components;

public sealed class ClinicTime(TimeProvider timeProvider)
{
    public DateOnly Today => DateOf(timeProvider.GetUtcNow());

    public DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(Local(instant).DateTime);

    public string Time(DateTimeOffset instant) => Local(instant).ToString("HH:mm", CultureInfo.InvariantCulture);

    public string DayAndTime(DateTimeOffset instant) =>
        Local(instant).ToString("d MMM · HH:mm", CultureInfo.InvariantCulture);

    public string WeekdayAndTime(DateTimeOffset instant) =>
        Local(instant).ToString("ddd d MMM · HH:mm", CultureInfo.InvariantCulture);

    public static string Day(DateOnly date) => date.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    public static string Weekday(DateOnly date) => date.ToString("ddd", CultureInfo.InvariantCulture);

    private DateTimeOffset Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, timeProvider.LocalTimeZone);
}
