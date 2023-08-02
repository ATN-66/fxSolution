/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                  ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    public ThresholdBar(double open, double close)
    {
        Open = open;
        Close = close;
        Direction = DetermineDirection(open, close);
    }

    public Symbol Symbol
    {
        get; init;
    }
    public DateTime DateTime
    {
        get; init;
    }
    public double Open
    {
        get; init;
    }
    public double Close
    {
        get; set;
    }
    public double Ask
    {
        get; set;
    }
    public double Bid
    {
        get; set;
    }

    public double Threshold
    {
        get;
        set;
    }

    public Direction Direction
    {
        get; init;
    }

    private static Direction DetermineDirection(double open, double close)
    {
        if (close > open)
        {
            return Direction.Up;
        }

        if (close < open)
        {
            return Direction.Down;
        }

        throw new Exception("DetermineDirection: Open and close cannot be equal.");
    }
}