/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                  ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Newtonsoft.Json;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    public ThresholdBar(double open, double close)
    {
        Open = open;
        Close = close;
        Direction = DetermineDirection(open, close);
    }

    [JsonIgnore]
    public Symbol Symbol
    {
        get; init;
    }

    public DateTime Start
    {
        get; init;
    }

    public DateTime End
    {
        get; set;
    }

    [JsonIgnore]
    public double Open
    {
        get; init;
    }

    [JsonIgnore]
    public double Close
    {
        get; set;
    }

    [JsonIgnore]
    public double Ask
    {
        get; set;
    }

    [JsonIgnore]
    public double Bid
    {
        get; set;
    }

    [JsonIgnore]
    public double Threshold
    {
        get;
        set;
    }

    [JsonIgnore]
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

    public override string ToString()
    {
        return $"{Start:D} {Start:T} - {End:D} {End:T} OC: {Open}, {Close}";
    }
}