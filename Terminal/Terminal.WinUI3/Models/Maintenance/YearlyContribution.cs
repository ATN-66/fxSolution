/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                            YearlyContribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public class YearlyContribution
{
    public int Year
    {
        get; set;
    }
    public List<MonthlyContribution> Months
    {
        get; set;
    }
}