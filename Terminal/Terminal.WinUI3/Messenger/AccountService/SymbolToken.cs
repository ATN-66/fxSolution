using Common.Entities;

namespace Terminal.WinUI3.Messenger.AccountService;

public sealed class SymbolToken : IEquatable<SymbolToken>
{
    public Symbol Symbol
    {
        get;
    }

    public SymbolToken(Symbol symbol)
    {
        Symbol = symbol;
    }

    public bool Equals(SymbolToken? other)
    {
        return other != null && Symbol == other.Symbol;
    }

    public override bool Equals(object? obj)
    {
        return obj is SymbolToken token && Equals(token);
    }

    public override int GetHashCode()
    {
        return (int)Symbol;
    }
}