/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

namespace Common.Entities;

public class Quotation : IChartItem, IComparable
{
    public Quotation(Symbol symbol, DateTime dateTime, double ask, double bid)
    {
        Symbol = symbol;
        Start = dateTime;
        Ask = ask;
        Bid = bid;
    }
    
    public Symbol Symbol { get; }
    public DateTime Start { get; }
    public DateTime End => Start;
    public double Ask { get; }
    public double Bid { get; }

    public static Quotation Empty => new(default, default, default, default);

    public int CompareTo(object? obj)
    {
        if (obj == null) return 1;
        var otherQuotation = (Quotation)obj;
        if (Start < otherQuotation.Start) return -1;
        if (Start > otherQuotation.Start) return 1;
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

    public double Price(Direction direction, int digit)
    {
        var tempNumber = Ask * digit;
        var lastDigit = (int)tempNumber % 10d;
        tempNumber = direction switch
        {
            Direction.Down when lastDigit <= 5d => Math.Floor(tempNumber / 10d) * 10d,
            Direction.Down => Math.Floor(tempNumber / 10d) * 10d + 5d,
            Direction.Up when lastDigit < 5d => Math.Floor(tempNumber / 10d) * 10d - 5d,
            Direction.Up => Math.Floor(tempNumber / 10d) * 10d,
            Direction.NaN => throw new ArgumentOutOfRangeException(nameof(direction), direction, @"invalid direction"),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, @"invalid direction")
        };

        var result = tempNumber / digit;
        return result;
    }

    public override string ToString()
    {
        return $"{Symbol}, {Start:D}, {Start:T}";
    }
}