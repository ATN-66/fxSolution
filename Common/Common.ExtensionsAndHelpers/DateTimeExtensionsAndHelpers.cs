/*+------------------------------------------------------------------+
  |                                      Common.ExtensionsAndHelpers |
  |                                  DateTimeExtensionsAndHelpers.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;

namespace Common.ExtensionsAndHelpers;

public static class DateTimeExtensionsAndHelpers
{
    public static readonly DateTime EpochStartDateTimeUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ElapsedSecondsFromJanuaryFirstOf1970(this DateTime dt)
    {
        return Convert.ToInt32((double)(dt.Ticks - EpochStartDateTimeUtc.Ticks) / TimeSpan.TicksPerSecond);
    }

    public static long ElapsedMinutesFromJanuaryFirstOf1970(this DateTime dt)
    {
        return (dt.Ticks - EpochStartDateTimeUtc.Ticks) / TimeSpan.TicksPerMinute;
    }

    public static int ElapsedMinutesSinceTheStartOfTheWeek(this DateTime dt)
    {
        var startOfWeek = dt.Date.AddDays(-(int)dt.DayOfWeek);
        var elapsed = dt - startOfWeek;
        return (int)elapsed.TotalMinutes;
    }

    public static List<DateTime> GetDatesInWeek(int year, int week)
    {
        var maxWeeksInYear = ISOWeek.GetWeeksInYear(year);

        if (week < 1 || week > maxWeeksInYear)
            throw new ArgumentOutOfRangeException(
                nameof(week),
                $"Week number must be between 1 and {maxWeeksInYear}.");

        var jan4 = new DateTime(year, 1, 4);
        var jan4DayOfWeek = (int)CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(jan4);
        var daysOffset = jan4DayOfWeek >= (int)DayOfWeek.Monday
            ? jan4DayOfWeek - (int)DayOfWeek.Monday
            : 7 - ((int)DayOfWeek.Monday - jan4DayOfWeek);
        var startOfFirstWeek = jan4.AddDays(-daysOffset);

        var startOfWeek = startOfFirstWeek.AddDays((week - 1) * 7);

        return Enumerable
            .Range(0, 7)
            .Select(i => startOfWeek.AddDays(i))
            .Where(date => date.Year == year)
            .ToList();
    }

    public static int Week(this DateTime dateTime)
    {
        var weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekNumber;
    }

    public static DateTime GetDateTime(this int elapsedSeconds)
    {
        return EpochStartDateTimeUtc.AddSeconds(elapsedSeconds);
    }

    public static int Quarter(int weekNumber)
    {
        const string errorMessage = $"{nameof(weekNumber)} is out of range.";
        return weekNumber switch
        {
            <= 0 => throw new Exception(errorMessage),
            <= 13 => 1,
            <= 26 => 2,
            <= 39 => 3,
            <= 52 => 4,
            _ => throw new Exception(errorMessage)
        };
    }

    public static DateTime SundayBeforeLastWeek()
    {
        var now = DateTime.Now;
        var currentDayOfWeek = ((int)now.DayOfWeek + 6) % 7 + 1; // Monday = 1, Sunday = 7 (ISO 8601)
        var startOfThisWeek = now.AddDays(-currentDayOfWeek + 1);
        var startOfWeekBeforeLast = startOfThisWeek.AddDays(-7); // Monday of the last week
        var sundayBeforeLast = startOfWeekBeforeLast.AddDays(-1); // Sunday before the last week
        return sundayBeforeLast.Date;
    }
}