/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    IProcessor.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.AI.Interfaces;

public interface IProcessor
{
    Task StartAsync(CancellationToken token);
}