/*+------------------------------------------------------------------+
  |                                      Common.ExtensionsAndHelpers |
  |                                  DateTimeExtensionsAndHelpers.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Globalization;
using Common.Entities;

namespace Common.ExtensionsAndHelpers;

public static class DateTimeExtensionsAndHelpers
{
    public static readonly DateTime EpochStartDateTimeUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static int ElapsedSecondsFromJanuaryFirstOf1970(this DateTime dt)
    {
        return Convert.ToInt32((double)(dt.Ticks - EpochStartDateTimeUtc.Ticks) / TimeSpan.TicksPerSecond);
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
        var weekNum = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekNum;
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

    public static void Check_ISO_8601(int yearNumber, int weekNumber, IList<Quotation> list)
    {
        var start = list[0].DateTime.Date;
        var end = list[^1].DateTime.Date;

        switch (yearNumber, weekNumber)
        {
            case (2022, 1):
                Debug.Assert(start >= DateTime.Parse("Monday, January 3, 2022") && end <= DateTime.Parse("Sunday, January 9, 2022"));
                break;
            case (2022, 2):
                Debug.Assert(start >= DateTime.Parse("Monday, January 10, 2022") && end <= DateTime.Parse("Sunday, January 16, 2022"));
                break;
            case (2022, 3):
                Debug.Assert(start >= DateTime.Parse("Monday, January 17, 2022") && end <= DateTime.Parse("Sunday, January 23, 2022"));
                break;
            case (2022, 4):
                Debug.Assert(start >= DateTime.Parse("Monday, January 24, 2022") && end <= DateTime.Parse("Sunday, January 30, 2022"));
                break;
            case (2022, 5):
                Debug.Assert(start >= DateTime.Parse("Monday, January 31, 2022") && end <= DateTime.Parse("Sunday, February 6, 2022"));
                break;
            case (2022, 6):
                Debug.Assert(start >= DateTime.Parse("Monday, February 7, 2022") && end <= DateTime.Parse("Sunday, February 13, 2022"));
                break;
            case (2022, 7):
                Debug.Assert(start >= DateTime.Parse("Monday, February 14, 2022") && end <= DateTime.Parse("Sunday, February 20, 2022"));
                break;
            case (2022, 8):
                Debug.Assert(start >= DateTime.Parse("Monday, February 21, 2022") && end <= DateTime.Parse("Sunday, February 27, 2022"));
                break;
            case (2022, 9):
                Debug.Assert(start >= DateTime.Parse("Monday, February 28, 2022") && end <= DateTime.Parse("Sunday, March 6, 2022"));
                break;
            case (2022, 10):
                Debug.Assert(start >= DateTime.Parse("Monday, March 7, 2022") && end <= DateTime.Parse("Sunday, March 13, 2022"));
                break;
            case (2022, 11):
                Debug.Assert(start >= DateTime.Parse("Monday, March 14, 2022") && end <= DateTime.Parse("Sunday, March 20, 2022"));
                break;
            case (2022, 12):
                Debug.Assert(start >= DateTime.Parse("Monday, March 21, 2022") && end <= DateTime.Parse("Sunday, March 27, 2022"));
                break;
            case (2022, 13):
                Debug.Assert(start >= DateTime.Parse("Monday, March 28, 2022") && end <= DateTime.Parse("Sunday, April 3, 2022"));
                break;
            case (2022, 14):
                Debug.Assert(start >= DateTime.Parse("Monday, April 4, 2022") && end <= DateTime.Parse("Sunday, April 10, 2022"));
                break;
            case (2022, 15):
                Debug.Assert(start >= DateTime.Parse("Monday, April 11, 2022") && end <= DateTime.Parse("Sunday, April 17, 2022"));
                break;
            case (2022, 16):
                Debug.Assert(start >= DateTime.Parse("Monday, April 18, 2022") && end <= DateTime.Parse("Sunday, April 24, 2022"));
                break;
            case (2022, 17):
                Debug.Assert(start >= DateTime.Parse("Monday, April 25, 2022") && end <= DateTime.Parse("Sunday, May 1, 2022"));
                break;
            case (2022, 18):
                Debug.Assert(start >= DateTime.Parse("Monday, May 2, 2022") && end <= DateTime.Parse("Sunday, May 8, 2022"));
                break;
            case (2022, 19):
                Debug.Assert(start >= DateTime.Parse("Monday, May 9, 2022") && end <= DateTime.Parse("Sunday, May 15, 2022"));
                break;
            case (2022, 20):
                Debug.Assert(start >= DateTime.Parse("Monday, May 16, 2022") && end <= DateTime.Parse("Sunday, May 22, 2022"));
                break;
            case (2022, 21):
                Debug.Assert(start >= DateTime.Parse("Monday, May 23, 2022") && end <= DateTime.Parse("Sunday, May 29, 2022"));
                break;
            case (2022, 22):
                Debug.Assert(start >= DateTime.Parse("Monday, May 30, 2022") && end <= DateTime.Parse("Sunday, June 5, 2022"));
                break;
            case (2022, 23):
                Debug.Assert(start >= DateTime.Parse("Monday, June 6, 2022") && end <= DateTime.Parse("Sunday, June 12, 2022"));
                break;
            case (2022, 24):
                Debug.Assert(start >= DateTime.Parse("Monday, June 13, 2022") && end <= DateTime.Parse("Sunday, June 19, 2022"));
                break;
            case (2022, 25):
                Debug.Assert(start >= DateTime.Parse("Monday, June 20, 2022") && end <= DateTime.Parse("Sunday, June 26, 2022"));
                break;
            case (2022, 26):
                Debug.Assert(start >= DateTime.Parse("Monday, June 27, 2022") && end <= DateTime.Parse("Sunday, July 3, 2022"));
                break;
            case (2022, 27):
                Debug.Assert(start >= DateTime.Parse("Monday, July 4, 2022") && end <= DateTime.Parse("Sunday, July 10, 2022"));
                break;
            case (2022, 28):
                Debug.Assert(start >= DateTime.Parse("Monday, July 11, 2022") && end <= DateTime.Parse("Sunday, July 17, 2022"));
                break;
            case (2022, 29):
                Debug.Assert(start >= DateTime.Parse("Monday, July 18, 2022") && end <= DateTime.Parse("Sunday, July 24, 2022"));
                break;
            case (2022, 30):
                Debug.Assert(start >= DateTime.Parse("Monday, July 25, 2022") && end <= DateTime.Parse("Sunday, July 31, 2022"));
                break;
            case (2022, 31):
                Debug.Assert(start >= DateTime.Parse("Monday, August 1, 2022") && end <= DateTime.Parse("Sunday, August 7, 2022"));
                break;
            case (2022, 32):
                Debug.Assert(start >= DateTime.Parse("Monday, August 8, 2022") && end <= DateTime.Parse("Sunday, August 14, 2022"));
                break;
            case (2022, 33):
                Debug.Assert(start >= DateTime.Parse("Monday, August 15, 2022") && end <= DateTime.Parse("Sunday, August 21, 2022"));
                break;
            case (2022, 34):
                Debug.Assert(start >= DateTime.Parse("Monday, August 22, 2022") && end <= DateTime.Parse("Sunday, August 28, 2022"));
                break;
            case (2022, 35):
                Debug.Assert(start >= DateTime.Parse("Monday, August 29, 2022") && end <= DateTime.Parse("Sunday, September 4, 2022"));
                break;
            case (2022, 36):
                Debug.Assert(start >= DateTime.Parse("Monday, September 5, 2022") && end <= DateTime.Parse("Sunday, September 11, 2022"));
                break;
            case (2022, 37):
                Debug.Assert(start >= DateTime.Parse("Monday, September 12, 2022") && end <= DateTime.Parse("Sunday, September 18, 2022"));
                break;
            case (2022, 38):
                Debug.Assert(start >= DateTime.Parse("Monday, September 19, 2022") && end <= DateTime.Parse("Sunday, September 25, 2022"));
                break;
            case (2022, 39):
                Debug.Assert(start >= DateTime.Parse("Monday, September 26, 2022") && end <= DateTime.Parse("Sunday, October 2, 2022"));
                break;
            case (2022, 40):
                Debug.Assert(start >= DateTime.Parse("Monday, October 3, 2022") && end <= DateTime.Parse("Sunday, October 9, 2022"));
                break;
            case (2022, 41):
                Debug.Assert(start >= DateTime.Parse("Monday, October 10, 2022") && end <= DateTime.Parse("Sunday, October 16, 2022"));
                break;
            case (2022, 42):
                Debug.Assert(start >= DateTime.Parse("Monday, October 17, 2022") && end <= DateTime.Parse("Sunday, October 23, 2022"));
                break;
            case (2022, 43):
                Debug.Assert(start >= DateTime.Parse("Monday, October 24, 2022") && end <= DateTime.Parse("Sunday, October 30, 2022"));
                break;
            case (2022, 44):
                Debug.Assert(start >= DateTime.Parse("Monday, October 31, 2022") && end <= DateTime.Parse("Sunday, November 6, 2022"));
                break;
            case (2022, 45):
                Debug.Assert(start >= DateTime.Parse("Monday, November 7, 2022") && end <= DateTime.Parse("Sunday, November 13, 2022"));
                break;
            case (2022, 46):
                Debug.Assert(start >= DateTime.Parse("Monday, November 14, 2022") && end <= DateTime.Parse("Sunday, November 20, 2022"));
                break;
            case (2022, 47):
                Debug.Assert(start >= DateTime.Parse("Monday, November 21, 2022") && end <= DateTime.Parse("Sunday, November 27, 2022"));
                break;
            case (2022, 48):
                Debug.Assert(start >= DateTime.Parse("Monday, November 28, 2022") && end <= DateTime.Parse("Sunday, December 4, 2022"));
                break;
            case (2022, 49):
                Debug.Assert(start >= DateTime.Parse("Monday, December 5, 2022") && end <= DateTime.Parse("Sunday, December 11, 2022"));
                break;
            case (2022, 50):
                Debug.Assert(start >= DateTime.Parse("Monday, December 12, 2022") && end <= DateTime.Parse("Sunday, December 18, 2022"));
                break;
            case (2022, 51):
                Debug.Assert(start >= DateTime.Parse("Monday, December 19, 2022") && end <= DateTime.Parse("Sunday, December 25, 2022"));
                break;
            case (2022, 52):
                Debug.Assert(start >= DateTime.Parse("Monday, December 26, 2022") && end <= DateTime.Parse("Friday, December 30, 2022"));
                break;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            case (2023, 1):
                Debug.Assert(start >= DateTime.Parse("Monday, January 2, 2023") && end <= DateTime.Parse("Sunday, January 8, 2023"));
                break;
            case (2023, 2):
                Debug.Assert(start >= DateTime.Parse("Monday, January 9, 2023") && end <= DateTime.Parse("Sunday, January 15, 2023"));
                break;
            case (2023, 3):
                Debug.Assert(start >= DateTime.Parse("Monday, January 16, 2023") && end <= DateTime.Parse("Sunday, January 22, 2023"));
                break;
            case (2023, 4):
                Debug.Assert(start >= DateTime.Parse("Monday, January 23, 2023") && end <= DateTime.Parse("Sunday, January 29, 2023"));
                break;
            case (2023, 5):
                Debug.Assert(start >= DateTime.Parse("Monday, January 30, 2023") && end <= DateTime.Parse("Sunday, February 5, 2023"));
                break;
            case (2023, 6):
                Debug.Assert(start >= DateTime.Parse("Monday, February 6, 2023") && end <= DateTime.Parse("Sunday, February 12, 2023"));
                break;
            case (2023, 7):
                Debug.Assert(start >= DateTime.Parse("Monday, February 13, 2023") && end <= DateTime.Parse("Sunday, February 19, 2023"));
                break;
            case (2023, 8):
                Debug.Assert(start >= DateTime.Parse("Monday, February 20, 2023") && end <= DateTime.Parse("Sunday, February 26, 2023"));
                break;
            case (2023, 9):
                Debug.Assert(start >= DateTime.Parse("Monday, February 27, 2023") && end <= DateTime.Parse("Sunday, March 5, 2023"));
                break;
            case (2023, 10):
                Debug.Assert(start >= DateTime.Parse("Monday, March 6, 2023") && end <= DateTime.Parse("Sunday, March 12, 2023"));
                break;
            case (2023, 11):
                Debug.Assert(start >= DateTime.Parse("Monday, March 13, 2023") && end <= DateTime.Parse("Sunday, March 19, 2023"));
                break;
            case (2023, 12):
                Debug.Assert(start >= DateTime.Parse("Monday, March 20, 2023") && end <= DateTime.Parse("Sunday, March 26, 2023"));
                break;
            case (2023, 13):
                Debug.Assert(start >= DateTime.Parse("Monday, March 27, 2023") && end <= DateTime.Parse("Sunday, April 2, 2023"));
                break;
            case (2023, 14):
                Debug.Assert(start >= DateTime.Parse("Monday, April 3, 2023") && end <= DateTime.Parse("Sunday, April 9, 2023"));
                break;
            case (2023, 15):
                Debug.Assert(start >= DateTime.Parse("Monday, April 10, 2023") && end <= DateTime.Parse("Sunday, April 16, 2023"));
                break;
            case (2023, 16):
                Debug.Assert(start >= DateTime.Parse("Monday, April 17, 2023") && end <= DateTime.Parse("Sunday, April 23, 2023"));
                break;
            case (2023, 17):
                Debug.Assert(start >= DateTime.Parse("Monday, April 24, 2023") && end <= DateTime.Parse("Sunday, April 30, 2023"));
                break;
            case (2023, 18):
                Debug.Assert(start >= DateTime.Parse("Monday, May 1, 2023") && end <= DateTime.Parse("Sunday, May 7, 2023"));
                break;
            case (2023, 19):
                Debug.Assert(start >= DateTime.Parse("Monday, May 8, 2023") && end <= DateTime.Parse("Sunday, May 14, 2023"));
                break;
            case (2023, 20):
                Debug.Assert(start >= DateTime.Parse("Monday, May 15, 2023") && end <= DateTime.Parse("Sunday, May 21, 2023"));
                break;
            case (2023, 21):
                Debug.Assert(start >= DateTime.Parse("Monday, May 22, 2023") && end <= DateTime.Parse("Sunday, May 28, 2023"));
                break;
            case (2023, 22):
                Debug.Assert(start >= DateTime.Parse("Monday, May 29, 2023") && end <= DateTime.Parse("Sunday, June 4, 2023"));
                break;
            case (2023, 23):
                Debug.Assert(start >= DateTime.Parse("Monday, June 5, 2023") && end <= DateTime.Parse("Sunday, June 11, 2023"));
                break;
            case (2023, 24):
                Debug.Assert(start >= DateTime.Parse("Monday, June 12, 2023") && end <= DateTime.Parse("Sunday, June 18, 2023"));
                break;
            case (2023, 25):
                Debug.Assert(start >= DateTime.Parse("Monday, June 19, 2023") && end <= DateTime.Parse("Sunday, June 25, 2023"));
                break;
            case (2023, 26):
                Debug.Assert(start >= DateTime.Parse("Monday, June 26, 2023") && end <= DateTime.Parse("Sunday, July 2, 2023"));
                break;
            case (2023, 27):
                Debug.Assert(start >= DateTime.Parse("Monday, July 3, 2023") && end <= DateTime.Parse("Sunday, July 9, 2023"));
                break;
            case (2023, 28):
                Debug.Assert(start >= DateTime.Parse("Monday, July 10, 2023") && end <= DateTime.Parse("Sunday, July 16, 2023"));
                break;
            case (2023, 29):
                Debug.Assert(start >= DateTime.Parse("Monday, July 17, 2023") && end <= DateTime.Parse("Sunday, July 23, 2023"));
                break;
            case (2023, 30):
                Debug.Assert(start >= DateTime.Parse("Monday, July 24, 2023") && end <= DateTime.Parse("Sunday, July 30, 2023"));
                break;
            case (2023, 31):
                Debug.Assert(start >= DateTime.Parse("Monday, July 31, 2023") && end <= DateTime.Parse("Sunday, August 6, 2023"));
                break;
            case (2023, 32):
                Debug.Assert(start >= DateTime.Parse("Monday, August 7, 2023") && end <= DateTime.Parse("Sunday, August 13, 2023"));
                break;
            case (2023, 33):
                Debug.Assert(start >= DateTime.Parse("Monday, August 14, 2023") && end <= DateTime.Parse("Sunday, August 20, 2023"));
                break;
            case (2023, 34):
                Debug.Assert(start >= DateTime.Parse("Monday, August 21, 2023") && end <= DateTime.Parse("Sunday, August 27, 2023"));
                break;
            case (2023, 35):
                Debug.Assert(start >= DateTime.Parse("Monday, August 28, 2023") && end <= DateTime.Parse("Sunday, September 3, 2023"));
                break;
            case (2023, 36):
                Debug.Assert(start >= DateTime.Parse("Monday, September 4, 2023") && end <= DateTime.Parse("Sunday, September 10, 2023"));
                break;
            case (2023, 37):
                Debug.Assert(start >= DateTime.Parse("Monday, September 11, 2023") && end <= DateTime.Parse("Sunday, September 17, 2023"));
                break;
            case (2023, 38):
                Debug.Assert(start >= DateTime.Parse("Monday, September 18, 2023") && end <= DateTime.Parse("Sunday, September 24, 2023"));
                break;
            case (2023, 39):
                Debug.Assert(start >= DateTime.Parse("Monday, September 25, 2023") && end <= DateTime.Parse("Sunday, October 1, 2023"));
                break;
            case (2023, 40):
                Debug.Assert(start >= DateTime.Parse("Monday, October 2, 2023") && end <= DateTime.Parse("Sunday, October 8, 2023"));
                break;
            case (2023, 41):
                Debug.Assert(start >= DateTime.Parse("Monday, October 9, 2023") && end <= DateTime.Parse("Sunday, October 15, 2023"));
                break;
            case (2023, 42):
                Debug.Assert(start >= DateTime.Parse("Monday, October 16, 2023") && end <= DateTime.Parse("Sunday, October 22, 2023"));
                break;
            case (2023, 43):
                Debug.Assert(start >= DateTime.Parse("Monday, October 23, 2023") && end <= DateTime.Parse("Sunday, October 29, 2023"));
                break;
            case (2023, 44):
                Debug.Assert(start >= DateTime.Parse("Monday, October 30, 2023") && end <= DateTime.Parse("Sunday, November 5, 2023"));
                break;
            case (2023, 45):
                Debug.Assert(start >= DateTime.Parse("Monday, November 6, 2023") && end <= DateTime.Parse("Sunday, November 12, 2023"));
                break;
            case (2023, 46):
                Debug.Assert(start >= DateTime.Parse("Monday, November 13, 2023") && end <= DateTime.Parse("Sunday, November 19, 2023"));
                break;
            case (2023, 47):
                Debug.Assert(start >= DateTime.Parse("Monday, November 20, 2023") && end <= DateTime.Parse("Sunday, November 26, 2023"));
                break;
            case (2023, 48):
                Debug.Assert(start >= DateTime.Parse("Monday, November 27, 2023") && end <= DateTime.Parse("Sunday, December 3, 2023"));
                break;
            case (2023, 49):
                Debug.Assert(start >= DateTime.Parse("Monday, December 4, 2023") && end <= DateTime.Parse("Sunday, December 10, 2023"));
                break;
            case (2023, 50):
                Debug.Assert(start >= DateTime.Parse("Monday, December 11, 2023") && end <= DateTime.Parse("Sunday, December 17, 2023"));
                break;
            case (2023, 51):
                Debug.Assert(start >= DateTime.Parse("Monday, December 18, 2023") && end <= DateTime.Parse("Sunday, December 24, 2023"));
                break;
            case (2023, 52):
                Debug.Assert(start >= DateTime.Parse("Monday, December 25, 2023") && end <= DateTime.Parse("Sunday, December 31, 2023"));
                break;
            default: throw new Exception();
        }
    }
}