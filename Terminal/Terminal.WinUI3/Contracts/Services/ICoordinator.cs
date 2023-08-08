/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    ICoordinator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.Contracts.Services;

public interface ICoordinator
{
    event EventHandler<PositionsEventArgs> PositionsUpdated;
    Task StartAsync(CancellationToken token);
    Task RequestTradingHistoryAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive);
    Task DoOpenPositionAsync(Symbol symbol, bool isReversed);
    Task DoClosePositionAsync(Symbol symbol, bool isReversed);
    void MainWindow_Closed(object sender, WindowEventArgs args);
}