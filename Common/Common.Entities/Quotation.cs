/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities;

public readonly record struct Quotation() : IChartItem, IComparable
{
    public Quotation(Symbol symbol, DateTime dateTime, double ask, double bid) : this()
    {
        Symbol = symbol;
        DateTime = dateTime;
        Ask = ask;
        Bid = bid;
    }
    
    public Symbol Symbol { get; }
    public DateTime DateTime { get; }
    public double Ask { get; }
    public double Bid { get; }

    public static Quotation Empty => new(default, default, default, default);

    public int CompareTo(object? obj)
    {
        if (obj == null) return 1;
        var otherQuotation = (Quotation)obj;
        if (DateTime < otherQuotation.DateTime) return -1;
        if (DateTime > otherQuotation.DateTime) return 1;
        if (Symbol < otherQuotation.Symbol) return -1;
        if (Symbol > otherQuotation.Symbol) return 1;
        throw new InvalidOperationException("The duplicates found.");
    }

    public string FormattedAsk
    {
        get
        {
            return Symbol switch
            {
                Symbol.EURUSD => Ask.ToString("0.00000"),
                Symbol.EURGBP => Ask.ToString("0.00000"),
                Symbol.GBPUSD => Ask.ToString("0.00000"),
                Symbol.USDJPY => Ask.ToString("000.000"),
                Symbol.EURJPY => Ask.ToString("000.000"),
                Symbol.GBPJPY => Ask.ToString("000.000"),
                _ => throw new Exception(nameof(Symbol))
            };
        }
    }

    public string FormattedBid
    {
        get
        {
            return Symbol switch
            {
                Symbol.EURUSD => Bid.ToString("0.00000"),
                Symbol.EURGBP => Bid.ToString("0.00000"),
                Symbol.GBPUSD => Bid.ToString("0.00000"),
                Symbol.USDJPY => Bid.ToString("000.000"),
                Symbol.EURJPY => Bid.ToString("000.000"),
                Symbol.GBPJPY => Bid.ToString("000.000"),
                _ => throw new Exception(nameof(Symbol))
            };
        }
    }

    public override string ToString()
    {
        return $"{Symbol}, {DateTime:D}, {DateTime:T}";
        //return Symbol switch
        //{
        //    Symbol.EURUSD => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
        //    Symbol.EURGBP => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
        //    Symbol.GBPUSD => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.00000}, {Bid:##0.00000}",
        //    Symbol.USDJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
        //    Symbol.EURJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
        //    Symbol.GBPJPY => $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {Ask:##0.000}, {Bid:##0.000}",
        //    _ => throw new Exception(nameof(Symbol))
        //};
    }
}