/*+------------------------------------------------------------------+
  |                                                   Common.Entities|
  |                                                    IChartItem.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities;

public interface IChartItem
{
    Symbol Symbol { get; }
    public DateTime Start { get; }
    public DateTime End { get; }
    public double Ask { get; }
    public double Bid { get; }
}