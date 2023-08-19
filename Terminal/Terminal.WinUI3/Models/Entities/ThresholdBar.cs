﻿/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                  ThresholdBar.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    public readonly double[] OpenArray = new double[2];
    public readonly double[] CloseArray = new double[2];

    public ThresholdBar(Force force, int id, double threshold, Symbol symbol, double open, double close, DateTime start, DateTime end)
    {
        Debug.Assert((force & Force.Extension) is not Force.Extension);
        Force = force;
        ID = id;
        Symbol = symbol;
        Open = open;
        Close = close;
        Start = start;
        End = end;
        Direction = DetermineDirection(open, close);

        if (Direction == Direction.Up)
        {
            Threshold = Close - threshold;
        }
        else
        {
            Threshold = Close + threshold;
        }
    }

    public int ID { get; }

    [JsonIgnore] public Symbol Symbol { get; }

    [JsonConverter(typeof(StringEnumConverter))] public Force Force { get; set; }

    public double Open
    {
        get => (Force & Force.Extension) == Force.Extension ? OpenArray[1] : OpenArray[0];
        private init
        {
            Debug.Assert((Force & Force.Extension) != Force.Extension);
            OpenArray[0] = OpenArray[1] = value;
        }
    }

    public double Close
    {
        get => (Force & Force.Extension) == Force.Extension ? CloseArray[1] : CloseArray[0];
        set
        {
            if ((Force & Force.Extension) == Force.Extension)
            {
                CloseArray[1] = value;
            }
            else
            {
                CloseArray[0] = CloseArray[1] = value;
            }
        }
    }

    [JsonIgnore] public double Threshold { get; set; }

    [JsonIgnore] public Direction Direction { get; init; }

    public DateTime Start { get; }

    public DateTime End { get; set; }

    [JsonIgnore] public double Ask { get; set; }

    [JsonIgnore] public double Bid { get; set; }

    public double Length => Math.Abs(Open - Close);

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
        $"{ID}, direction:{Direction}, force: {Force}";
}