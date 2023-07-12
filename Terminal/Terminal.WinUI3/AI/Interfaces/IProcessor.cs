/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    IProcessor.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.AI.Interfaces;

public interface IProcessor
{
    Task StartAsync(CancellationToken token);
    Task DownAsync(Symbol symbol, bool isReversed);
    Task ExitAsync();
}