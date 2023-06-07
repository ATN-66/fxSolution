/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                            HourlyContribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public class HourlyContribution
{
    public HourlyContribution(long hour, DateTime dateTime, bool hasContribution)
    {
        Hour = hour;
        DateTime = dateTime;
        HasContribution = hasContribution;
    }

    public long Hour
    {
        get; private set;
    }

    public DateTime DateTime
    {
        get; private set;
    }

    public bool HasContribution
    {
        get; private set;
    }
}