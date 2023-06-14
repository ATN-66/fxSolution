/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                          SymbolicContribution.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Maintenance;

public sealed class SymbolicContribution : DailyContribution
{
    public Symbol Symbol
    {
        get; init;
    }

    public override string ToString()
    {
        return $"symbol:{Symbol}, year:{Year}, month:{Month}, week:{Week}, day:{Day}, contribution:{Contribution}";
    }
}
