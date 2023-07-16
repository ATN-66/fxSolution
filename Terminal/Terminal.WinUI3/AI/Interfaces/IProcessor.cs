/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    IProcessor.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Models;

namespace Terminal.WinUI3.AI.Interfaces;

public interface IProcessor
{
    event EventHandler<PositionsEventArgs> PositionsUpdated;
    Task StartAsync(CancellationToken token);
    void RequestPositionsAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive);
    Task OpenPositionAsync(Symbol symbol, bool isReversed);
    Task ClosePositionAsync(Symbol symbol, bool isReversed);
    Task ExitAsync();
}