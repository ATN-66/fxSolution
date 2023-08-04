/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                  ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Newtonsoft.Json;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    public ThresholdBar(int id, double open, double close)
    {
        Id = id;
        Open = open;
        Close = close;
        Direction = DetermineDirection(open, close);
    }

    public int Id
    {
        get; init;
    }

    [JsonIgnore]
    public Symbol Symbol
    {
        get; init;
    }

    [JsonIgnore]
    public DateTime StartDateTime
    {
        get; init;
    }

    [JsonIgnore]
    public DateTime EndDateTime
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
        return $"{StartDateTime:yyyy-MM-dd HH:mm}-{EndDateTime:yyyy-MM-dd HH:mm} OC: {Open}, {Close}";
    }
}