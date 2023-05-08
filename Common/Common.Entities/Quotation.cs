/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

using System;

namespace Common.Entities;

public readonly record struct Quotation(Symbol Symbol, DateTime DateTime, double Ask, double Bid) : IComparable
{
    public readonly Symbol Symbol = Symbol;
    public readonly DateTime DateTime = DateTime;
    public readonly double Ask = Ask;
    public readonly double Bid = Bid;

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        var otherQuotation = (Quotation)obj;
        if (Symbol < otherQuotation.Symbol) return -1;
        if (Symbol > otherQuotation.Symbol) return 1;
        if (DateTime < otherQuotation.DateTime) return -1;
        if (DateTime > otherQuotation.DateTime) return 1;
        throw new Exception();
    }

    public override string ToString()
    {
        return $"{Symbol}|{DateTime:yyyy.MM.dd HH:mm:s.fff}|{Ask:D6}|{Bid:D6}";
    }
}