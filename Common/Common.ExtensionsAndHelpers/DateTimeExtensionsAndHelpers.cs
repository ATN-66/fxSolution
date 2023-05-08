/*+------------------------------------------------------------------+
  |                                      Common.ExtensionsAndHelpers |
  |                                  DateTimeExtensionsAndHelpers.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;

namespace Common.ExtensionsAndHelpers;

public static class DateTimeExtensionsAndHelpers
{
    private static readonly DateTime ZeroDateTime = new(1970, 1, 1);

    public static int ElapsedSecondsFromJanuaryFirstOf1970(this DateTime dt)
    {
        return Convert.ToInt32((double)(dt.Ticks - ZeroDateTime.Ticks) / TimeSpan.TicksPerSecond);
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

    public static int WeekOfYear(this DateTime dt)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        const CalendarWeekRule calendarWeekRule = CalendarWeekRule.FirstFourDayWeek;
        const DayOfWeek firstDayOfWeek = DayOfWeek.Monday;

        return cal.GetWeekOfYear(dt, calendarWeekRule, firstDayOfWeek);
    }

    public static DateTime GetDateTime(this int elapsedSeconds)
    {
        return ZeroDateTime.AddSeconds(elapsedSeconds);
    }
}