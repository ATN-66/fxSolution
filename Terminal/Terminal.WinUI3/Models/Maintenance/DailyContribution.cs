/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                             DailyContribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public record struct DailyContribution
{
    public DailyContribution(DateTime date, bool hasContribution)
    {
        Date = date;
        HasContribution = hasContribution;
    }

    public DateTime Date
    {
        get; set;
    }

    public bool HasContribution
    {
        get; set;
    }

    public string FormatDate(DateTime date)
    {
        return date.Day.ToString("00");
    }

    public override string ToString()
    {
        return $"{Date:D}, {HasContribution}";
    }
}