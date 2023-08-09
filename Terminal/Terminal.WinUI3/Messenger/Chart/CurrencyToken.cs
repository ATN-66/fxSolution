using Common.Entities;

namespace Terminal.WinUI3.Messenger.Chart;

public sealed class CurrencyToken : IEquatable<CurrencyToken>
{
    public Currency Currency
    {
        get;
    }

    public CurrencyToken(Currency currency)
    {
        Currency = currency;
    }

    public bool Equals(CurrencyToken? other)
    {
        return other != null && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        return obj is CurrencyToken token && Equals(token);
    }

    public override int GetHashCode()
    {
        return (int)Currency;
    }
}