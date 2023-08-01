/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                       TradingHistoryViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Trade;
using ICoordinator = Terminal.WinUI3.Contracts.Services.ICoordinator;

namespace Terminal.WinUI3.ViewModels;

public partial class TradingHistoryViewModel : ObservableRecipient, INavigationAware
{
    private readonly ICoordinator _coordinator;
    //private readonly IDispatcherService _dispatcherService;
    [ObservableProperty] private string _headerContext = "Trading History";
    [ObservableProperty] private ObservableCollection<HistoryPosition> _positions = null!;
    [ObservableProperty] private Position _selectedPosition = null!;
    [ObservableProperty] private DateTimeOffset _selectedDate;
    public DateTimeOffset Today { get; } = DateTimeOffset.Now;

    public TradingHistoryViewModel(ICoordinator coordinator, IDispatcherService dispatcherService)
    {
        _coordinator = coordinator;
        //_dispatcherService = dispatcherService;

        _coordinator.PositionsUpdated += delegate(object? _, PositionsEventArgs args)
        {
            dispatcherService.ExecuteOnUIThreadAsync(() =>
                           {
                Positions = new ObservableCollection<HistoryPosition>(args.Positions);
            });
        };
    }

    public void OnNavigatedTo(object parameter)
    {
        var end = DateTime.Now.Date;
        var start = end.AddDays(-31).Date;
        _coordinator.RequestTradingHistoryAsync(start, end);
    }

    public void OnNavigatedFrom()
    {

    }
}