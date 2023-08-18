/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                  ThresholdBar.cs |
  +------------------------------------------------------------------+*/


using Common.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    public ThresholdBar(int id, double open, double close)
    {
        ID = id;
        Open = open;
        Close = close;
        Direction = DetermineDirection(open, close);
    }

    private int ID
    {
        get;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public Force UpForce
    {
        get;
        set;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public Force DownForce
    {
        get;
        set;
    }

    //[JsonIgnore]
    public double Open
    {
        get;
        init;
    }

    //[JsonIgnore]
    public double Close
    {
        get;
        set;
    }

    [JsonIgnore]
    public double Threshold
    {
        get;
        set;
    }

    [JsonIgnore] //[JsonConverter(typeof(StringEnumConverter))]
    public Direction Direction
    {
        get;
        init;
    }

    [JsonIgnore]
    public Symbol Symbol
    {
        get;
        init;
    }

    //[JsonIgnore]
    public DateTime Start
    {
        get;
        init;
    }

    //[JsonIgnore]
    public DateTime End
    {
        get;
        set;
    }

    [JsonIgnore]
    public double Ask
    {
        get;
        set;
    }

    [JsonIgnore]
    public double Bid
    {
        get;
        set;
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

    public override string ToString() =>
        //return $"{Start:yyyy-MM-dd}, from: {Start:HH:mm:ss} to: {End:HH:mm:ss}; OC: {Open}, {Close}";
        $"{ID}, up: {UpForce}, down: {DownForce}";
}