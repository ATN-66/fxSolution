/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                          MonthlyContributions.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public class MonthlyContributions
{
    public int Year
    {
        get; set;
    }

    public int Month
    {
        get; set;
    }

    public List<Contribution> Contributions
    {
        get; set;
    }
}