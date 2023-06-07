/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                             DailyContribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public class DailyContribution
{
    public DailyContribution(DateTime dateTime, Contribution contribution)
    {
        DateTime = dateTime;
        Contribution = contribution;
    }

    public DateTime DateTime
    {
        get; private set;
    }

    public Contribution Contribution
    {
        get; set;
    }

    public List<HourlyContribution> HourlyContributions
    {
        get; set;
    }

    public string FormatDate(DateTime date)
    {
        return date.Day.ToString("00");
    }

    public override string ToString()
    {
        return $"{DateTime:D}, {Contribution}";
    }
}