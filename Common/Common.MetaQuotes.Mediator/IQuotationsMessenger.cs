/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                          IQuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

namespace Common.MetaQuotes.Mediator;

public interface IQuotationsMessenger
{
    void DeInit(int symbol, int reason);
    string Init(int id, int symbol, string datetime, double ask, double bid, int environment);
    string Tick(int id, int symbol, string datetime, double ask, double bid);
}