/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                   Candlestick.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using Common.Entities;

namespace Terminal.WinUI3.AI.Data;

public readonly record struct Candlestick: IChartItem
{
    public Symbol Symbol
    {
        get; init;
    }
    public DateTime DateTime
    {
        get; init;
    }
    public double Ask
    {
        get; init;
    }
    public double Bid
    {
        get; init;
    }

    [Description("Elapsed Minutes From January First Of 1970")]
    public long Minutes
    {
        get; init;
    }
    public double Open
    {
        get; init;
    }
    public double Close
    {
        get; init;
    }
    public double High
    {
        get; init;
    }
    public double Low
    {
        get; init;
    }
}