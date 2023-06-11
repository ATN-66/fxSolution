/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

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

    //public override string ToString()
    //{
    //    switch (Symbol)
    //    {
    //        case Symbol.EURUSD:
    //        case Symbol.EURGBP:
    //        case Symbol.GBPUSD:
    //            return $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}";
    //        case Symbol.USDJPY:
    //        case Symbol.EURJPY:
    //        case Symbol.GBPJPY:
    //            return $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}";
    //        default: throw new Exception(nameof(Symbol));
    //    }
    //}

    public override string ToString()
    {
        return $"{Symbol}, {DateTime:D}, {DateTime:T}";
    }
}