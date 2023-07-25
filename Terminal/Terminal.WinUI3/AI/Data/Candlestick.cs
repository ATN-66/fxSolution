/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                   Candlestick.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using Common.Entities;

namespace Terminal.WinUI3.AI.Data;

public class Candlestick: IChartItem
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
        get; set;
    }
    public double Bid
    {
        get; set;
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
        get; set;
    }
    public double High
    {
        get; set;
    }
    public double Low
    {
        get; set;
    }
}