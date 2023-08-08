/*+------------------------------------------------------------------+
  |                                     Terminal.WinUI3.Models.Chart |
  |                                                 ChartSettings.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Chart;

public struct ChartSettings
{
    public bool IsDefault { get; set; }

    public Symbol Symbol { get; init; }
    public bool IsReversed { get; init; }
    public ChartType ChartType { get; set; }

    public int HorizontalShift { get; init; }
    public double VerticalShift { get; init; }
    public int KernelShiftPercent { get; set; }
}