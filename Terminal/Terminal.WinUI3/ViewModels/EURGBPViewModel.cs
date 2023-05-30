/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               EURGBPViewModel.cs |
  +------------------------------------------------------------------+*/


using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.ViewModels;

public partial class EURGBPViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "EURGBPViewModel_HeaderContext".GetLocalized();
    private TickChartControl? _tickTickChartControlControl;

    public EURGBPViewModel()
    {
        var visualService = App.GetService<IVisualService>();
        _tickTickChartControlControl = visualService.GetTickChartControl(Symbol.EURGBP, false);
    }

    public UIElement? TickChartControl => _tickTickChartControlControl;

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        _tickTickChartControlControl?.Detach();
        _tickTickChartControlControl = null;
    }
}