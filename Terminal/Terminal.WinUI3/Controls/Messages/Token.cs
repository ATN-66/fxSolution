using Common.Entities;

namespace Terminal.WinUI3.Controls.Messages;

public sealed class Token : IEquatable<Token>
{
    public Symbol Symbol
    {
        get;
    }

    public Token(Symbol symbol)
    {
        Symbol = symbol;
    }

    public bool Equals(Token? other)
    {
        return other != null && Symbol == other.Symbol;
    }

    public override bool Equals(object? obj)
    {
        return obj is Token token && Equals(token);
    }

    public override int GetHashCode()
    {
        return (int)Symbol;
    }
}