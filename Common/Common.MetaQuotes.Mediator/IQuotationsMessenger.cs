/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                          IQuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Common.MetaQuotes.Mediator;

public interface IQuotationsMessenger
{
    void DeInit(Symbol symbol, DeInitReason reason);
    string Init(int symbol, string datetime, int ask, int bid, int environment);
    string Tick(int symbol, string datetime, int ask, int bid);
}