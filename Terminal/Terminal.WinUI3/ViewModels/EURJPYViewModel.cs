/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               EURJPYViewModel.cs |
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

public partial class EURJPYViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "EURJPYViewModel_HeaderContext".GetLocalizedString();
    private TickChartControl? _tickTickChartControlControl;

    public EURJPYViewModel()
    {
        var visualService = App.GetService<IVisualService>();
        _tickTickChartControlControl = visualService.GetTickChartControl(Symbol.EURJPY, false);
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