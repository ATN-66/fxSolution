﻿/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                   Candlestick.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using System.Numerics;
using Common.Entities;

namespace Terminal.WinUI3.Models.Entities;

public class Candlestick : IChartItem
{
    public Symbol Symbol { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime => StartDateTime;

    [Description("Elapsed Minutes From January First Of 1970")]
    public long Minutes { get; init; }
    public double Open { get; init; }
    public double Close { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Ask { get; set; }
    public double Bid { get; set; }

    public Vector2 OpenPoint { get; set; }
    public Vector2 ClosePoint { get; set; }
    public Vector2 HighPoint { get; set; }
    public Vector2 LowPoint { get; set; }

    public override string ToString()
    {
        return $"{Symbol} {StartDateTime:D} {StartDateTime:T} OCHL: {Open}, {Close}, {High}, {Low}";
    }
}