﻿/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Messenger.Chart |
  |                                                 CurrencyToken.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Messenger.Chart;

public sealed class CurrencyToken : CommunicationToken
{
    public Currency Currency
    {
        get;
    }

    public CurrencyToken(Currency currency)
    {
        Currency = currency;
    }

    public override bool Equals(CommunicationToken? other)
    {
        return other is CurrencyToken token && Currency == token.Currency;
    }

    public override int GetHashCode()
    {
        return (int)Currency;
    }
}