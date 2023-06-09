/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                            HourlyContribution.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;

namespace Terminal.WinUI3.Models.Maintenance;

public record struct HourlyContribution
{
    public int Year => DateTime.Year;

    public int Month => DateTime.Month;

    public int Week => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

    public int Day => DateTime.Day;

    public long Hour //from db
    {
        get; init;
    }

    public DateTime DateTime // from db
    {
        get; init;
    }

    public bool HasContribution
    {
        get; init;
    }

    public override string ToString()
    {
        return $"y:{Year}, m:{Month}, w:{Week}, d:{Day}, , h:{Hour}, c:{HasContribution}";
    }
}