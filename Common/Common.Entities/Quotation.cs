/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                                     Quotation.cs |
  +------------------------------------------------------------------+*/

using System;

namespace Common.Entities;

public readonly record struct Quotation(Symbol Symbol, DateTime DateTime, int Ask, int Bid) : IComparable
{
    public readonly Symbol Symbol = Symbol;
    public readonly DateTime DateTime = DateTime;
    public readonly int Ask = Ask;
    public readonly int Bid = Bid;

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
        return $"{Symbol}|{DateTime:dd.MM.yyyy HH:mm:ss.fff}|{Ask:D6}|{Bid:D6}";
    }
}