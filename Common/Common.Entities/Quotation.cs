/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities;

public readonly record struct Quotation() : IComparable
{
    public readonly int ID;
    public readonly Symbol Symbol;
    public readonly DateTime DateTime;
    public readonly double DoubleAsk;
    public readonly double DoubleBid;
    public readonly int IntAsk;
    public readonly int IntBid;

    public Quotation(int id, Symbol symbol, DateTime dateTime, double doubleAsk, double doubleBid, int intAsk, int intBid) : this()
    {
        ID = id;
        Symbol = symbol;
        DateTime = dateTime;
        DoubleAsk = doubleAsk;
        DoubleBid = doubleBid;
        IntAsk = intAsk;
        IntBid = intBid;
    }

    public static Quotation Empty => new(default, default, default, default, default, default, default);

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

    public override string ToString()
    {
        switch (Symbol)
        {
            case Symbol.EURUSD:
            case Symbol.EURGBP:
            case Symbol.GBPUSD:
                return $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {DoubleAsk:##0.00000}, {DoubleBid:##0.00000}, {IntAsk:00000}, {IntBid:00000}";
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                return $"{ID:000000}, {Symbol}, {DateTime:HH:mm:ss.fff}, {DoubleAsk:##0.000}, {DoubleBid:##0.000}, {IntAsk:00000}, {IntBid:00000}";
            default: throw new Exception(nameof(Symbol));
        }
    }
}