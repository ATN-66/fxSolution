/*+------------------------------------------------------------------+
  |                                                   Common.Entities|
  |                                                    IChartItem.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities
{
    public interface IChartItem
    {
        Symbol Symbol { get; }
        public DateTime DateTime { get; }
        public double Ask { get; }
        public double Bid { get; }
    }
}