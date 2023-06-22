/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;

namespace Common.Entities;

public readonly record struct Quotation() : IComparable
{
    public readonly int ID;

    public Quotation(int id, Symbol symbol, DateTime dateTime, double ask, double bid) : this()
    {
        ID = id;
        Symbol = symbol;
        DateTime = dateTime;
        Ask = ask;
        Bid = bid;
    }
    
    public Symbol Symbol { get; init; }
    public DateTime DateTime { get; }
    public double Ask { get; }
    public double Bid { get; }

    public static Quotation Empty => new(default, default, default, default, default);

    public int Week => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

    public int Quarter
    {
        get
        {
            return Week switch
            {
                <= 0 => throw new InvalidOperationException(nameof(Week)),
                <= 13 => 1,
                <= 26 => 2,
                <= 39 => 3,
                <= 52 => 4,
                _ => throw new InvalidOperationException(nameof(Week))
            };
        }
    }

    public int CompareTo(object? obj)
    {
        if (obj == null) return 1;
        var otherQuotation = (Quotation)obj;
        if (DateTime < otherQuotation.DateTime) return -1;
        if (DateTime > otherQuotation.DateTime) return 1;
        if (Symbol < otherQuotation.Symbol) return -1;
        if (Symbol > otherQuotation.Symbol) return 1;
        return 0;
    }

    public string FormattedAsk
    {
        get
        {
            switch (Symbol)
            {
                case Symbol.EURUSD:
                case Symbol.EURGBP:
                case Symbol.GBPUSD:
                    return Ask.ToString("0.00000");
                case Symbol.USDJPY:
                case Symbol.EURJPY:
                case Symbol.GBPJPY:
                    return Ask.ToString("000.000");
                default: throw new Exception(nameof(Symbol));
            }
        }
    }

    public string FormattedBid
    {
        get
        {
            switch (Symbol)
            {
                case Symbol.EURUSD:
                case Symbol.EURGBP:
                case Symbol.GBPUSD:
                    return Bid.ToString("0.00000");
                case Symbol.USDJPY:
                case Symbol.EURJPY:
                case Symbol.GBPJPY:
                    return Bid.ToString("000.000");
                default: throw new Exception(nameof(Symbol));
            }
        }
    }

    public override string ToString()
    {
        //return $"{Symbol}, {DateTime:D}, {DateTime:T}";
        return Symbol switch
        {
            Symbol.EURUSD => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
            Symbol.EURGBP => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
            Symbol.GBPUSD => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
            Symbol.USDJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
            Symbol.EURJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
            Symbol.GBPJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
            _ => throw new Exception(nameof(Symbol))
        };
    }
}

