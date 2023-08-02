/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    ICoordinator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.Contracts.Services;

public interface ICoordinator
{
    event EventHandler<PositionsEventArgs> PositionsUpdated;
    Task StartAsync(CancellationToken token);
    Task RequestTradingHistoryAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive);
    Task DoOpenPositionAsync(Symbol symbol, bool isReversed);
    Task DoClosePositionAsync(Symbol symbol, bool isReversed);
    Task ExitAsync();
}