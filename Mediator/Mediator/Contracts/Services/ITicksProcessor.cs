/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                               ITicksProcessor.cs |
  +------------------------------------------------------------------+*/

namespace Mediator.Contracts.Services;

public interface ITicksProcessor
{
    Task DeInitAsync(int reason);
    Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace);
    string Tick(int id, int symbol, string datetime, double ask, double bid);
}