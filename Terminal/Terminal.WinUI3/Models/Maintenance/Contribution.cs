/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                                  Contribution.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

public record struct Contribution
{
    public Contribution(DateTime date, bool hasContribution)
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

    public override string ToString()
    {
        return $"{Date:D}, {HasContribution}";
    }
}