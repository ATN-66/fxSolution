/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                  QuotationKey.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities;

public readonly struct QuotationKey
{
    public Symbol Symbol
    {
        get; init;
    }
    public int Year
    {
        get; init;
    }
    public int Month
    {
        get; init;
    }
    public int Day
    {
        get; init;
    }
    public int Hour
    {
        get; init;
    }
    public int Minute
    {
        get; init;
    }
    public override string ToString()
    {
        return $"{Symbol}|{Year:0000}|{Month:00}|{Day:00}|{Hour:00}|{Minute:00}";
    }
}