/*+------------------------------------------------------------------+
  |                                     Terminal.WinUI3.Models.Chart |
  |                                                 ChartSettings.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Chart;

public struct ChartSettings
{
    public bool IsDefault { get; set; }

    public ChartType ChartType { get; init; }
    public Symbol Symbol { get; init; }
    public bool IsReversed { get; init; }

    public int HorizontalShift { get; init; }
    public double VerticalShift { get; init; }
    public int KernelShiftPercent { get; set; }
}