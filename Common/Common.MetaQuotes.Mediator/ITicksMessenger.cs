/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                          IQuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

using System.Threading.Tasks;

namespace Common.MetaQuotes.Mediator;

public interface ITicksMessenger
{
    void DeInit(int symbol, int reason);
    Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace);
    string Tick(int id, int symbol, string datetime, double ask, double bid);
}