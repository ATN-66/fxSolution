/*+------------------------------------------------------------------+
  |                                                   Common.Entities|
  |                                                    IChartItem.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities
{
    public interface IChartItem
    {
        Symbol Symbol { get; }
        public DateTime StartDateTime { get; }
        public DateTime EndDateTime { get; }
        public double Ask { get; }
        public double Bid { get; }
    }
}