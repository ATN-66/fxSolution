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
    private BaseChartControl? _baseChartControl;

    public EURGBPViewModel()
    {
        var visualService = App.GetService<IVisualService>();
        _baseChartControl = visualService.GetChartControl(Symbol.EURGBP, false);
    }

    public UIElement? Chart => _baseChartControl;

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        _baseChartControl?.Detach();
        _baseChartControl = null;
    }
}