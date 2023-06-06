/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                           MonthlyContribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public class MonthlyContribution
{
    public int Month
    {
        get; set;
    }

    public List<DailyContribution> DailyContributions
    {
        get; set;
    }
}