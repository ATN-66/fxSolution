/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               USDGBPViewModel.cs |
  +------------------------------------------------------------------+*/


using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.ViewModels;

public partial class USDGBPViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "USDGBPViewModel_HeaderContext".GetLocalizedString();
    private TickChartControl? _tickChartControl;

    public USDGBPViewModel()
    {
        var visualService = App.GetService<IVisualService>();
        _tickChartControl = visualService.GetTickChartControl(Symbol.GBPUSD, true);
    }

    public UIElement? TickChartControl => _tickChartControl;

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        _tickChartControl?.Detach();
        _tickChartControl = null;
    }
}