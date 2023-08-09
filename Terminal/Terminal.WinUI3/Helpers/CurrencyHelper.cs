using Common.Entities;

namespace Terminal.WinUI3.Helpers;

public static class CurrencyHelper
{
    public static Currency GetCurrency(Symbol symbol, bool isReversed)
    {
        var symbolName = symbol.ToString();
        var first = symbolName[..3];
        var second = symbolName.Substring(3, 3);

        if (Enum.TryParse<Currency>(first, out var baseCurrency) && Enum.TryParse<Currency>(second, out var quoteCurrency))
        {
            return isReversed ? baseCurrency : quoteCurrency;
        }

        throw new Exception("Failed to parse currencies from symbol.");
    }
}