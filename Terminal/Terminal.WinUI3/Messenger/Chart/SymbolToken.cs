/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Messenger.Chart |
  |                                                   SymbolToken.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Messenger.Chart;

public sealed class SymbolToken : CommunicationToken
{
    public Symbol Symbol
    {
        get;
    }

    public SymbolToken(Symbol symbol)
    {
        Symbol = symbol;
    }

    public override bool Equals(CommunicationToken? other)
    {
        return other is SymbolToken token && Symbol == token.Symbol;
    }

    public override int GetHashCode()
    {
        return (int)Symbol;
    }
}