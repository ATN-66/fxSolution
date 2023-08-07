/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                   Candlestick.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Entities;

public class Candlestick : IChartItem
{
    public Symbol Symbol { get; init; }
    public DateTime Start { get; init; }
    public DateTime End => Start;
    public double Open { get; init; }
    public double Close { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Ask { get; set; }
    public double Bid { get; set; }

    public override string ToString()
    {
        return $"{Start:D} {Start:T} OCHL: {Open}, {Close}, {High}, {Low}";
    }
}