/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                     DailyBySymbolContribution.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Maintenance;

public sealed class DailyBySymbolContribution : DailyContribution
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
