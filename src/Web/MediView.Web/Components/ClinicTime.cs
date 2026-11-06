using System.Globalization;

namespace MediView.Web.Components;

public static class ClinicTime
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    public static DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToLocalTime().DateTime);

    public static string Time(DateTimeOffset instant) => Format(instant, "HH:mm");

    public static string DayAndTime(DateTimeOffset instant) => Format(instant, "d MMM · HH:mm");

    public static string WeekdayAndTime(DateTimeOffset instant) => Format(instant, "ddd d MMM · HH:mm");

    public static string Day(DateOnly date) => date.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    public static string Weekday(DateOnly date) => date.ToString("ddd", CultureInfo.InvariantCulture);

    private static string Format(DateTimeOffset instant, string pattern) =>
        instant.ToLocalTime().ToString(pattern, CultureInfo.InvariantCulture);
}
