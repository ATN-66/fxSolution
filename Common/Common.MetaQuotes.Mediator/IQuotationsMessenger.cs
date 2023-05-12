/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                          IQuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

using System.Threading.Tasks;
using Common.Entities;

namespace Common.MetaQuotes.Mediator;

public interface IQuotationsMessenger
{
    void DeInit(Symbol symbol, DeInitReason reason);
    string Init(int symbol, string datetime, double ask, double bid, int environment);
    Task<string> TickAsync(int symbol, string datetime, double ask, double bid);
}