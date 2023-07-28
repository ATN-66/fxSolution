/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                       TradingHistoryViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.AI.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Trade;

namespace Terminal.WinUI3.ViewModels;

public partial class TradingHistoryViewModel : ObservableRecipient, INavigationAware
{
    private readonly IProcessor _processor;
    //private readonly IDispatcherService _dispatcherService;
    [ObservableProperty] private string _headerContext = "Trading History";
    [ObservableProperty] private ObservableCollection<HistoryPosition> _positions = null!;
    [ObservableProperty] private Position _selectedPosition = null!;
    [ObservableProperty] private DateTimeOffset _selectedDate;
    public DateTimeOffset Today { get; } = DateTimeOffset.Now;

    public TradingHistoryViewModel(IProcessor processor, IDispatcherService dispatcherService)
    {
        _processor = processor;
        //_dispatcherService = dispatcherService;

        _processor.PositionsUpdated += delegate(object? _, PositionsEventArgs args)
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
        _processor.RequestTradingHistoryAsync(start, end);
    }

    public void OnNavigatedFrom()
    {

    }
}