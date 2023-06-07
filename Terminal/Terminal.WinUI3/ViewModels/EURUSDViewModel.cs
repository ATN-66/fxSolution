/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               EURUSDViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.ViewModels;

public partial class EURUSDViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "EURUSDViewModel_HeaderContext".GetLocalizedString();
    [ObservableProperty] private float _pipsPerChart = 100;//settings
    [ObservableProperty] private float _maxPipsPerChart = 200;//settings
    [ObservableProperty] private float _minPipsPerChart = 10;//settings
    [ObservableProperty] private int _unitsPerChart = 500;//settings
    [ObservableProperty] private int _maxUnitsPerChart = int.MaxValue;
    [ObservableProperty] private int _minUnitsPerChart = 10;//settings
    [ObservableProperty] private double _kernelShiftPercent = 100;

    public TickChartControl TickChartControl
    {
        get;
        private set;
    }

    public EURUSDViewModel()
    {
        var visualService = App.GetService<IVisualService>();
        TickChartControl = visualService.GetTickChartControl(Symbol.EURUSD, false)!;
        TickChartControl.DataContext = this;
        TickChartControl.SetBinding(TickChartControl.PipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(PipsPerChart)), Mode = BindingMode.TwoWay });
        TickChartControl.SetBinding(TickChartControl.MaxPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxPipsPerChart)), Mode = BindingMode.OneWay });
        TickChartControl.SetBinding(TickChartControl.MinPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinPipsPerChart)), Mode = BindingMode.OneWay });
        TickChartControl.SetBinding(TickChartControl.UnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPerChart)), Mode = BindingMode.TwoWay });
        TickChartControl.SetBinding(TickChartControl.MaxUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxUnitsPerChart)), Mode = BindingMode.TwoWay });
        TickChartControl.SetBinding(TickChartControl.MinUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnitsPerChart)), Mode = BindingMode.OneWay });
        TickChartControl.SetBinding(TickChartControl.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
    }

    partial void OnPipsPerChartChanged(float value)
    {
        TickChartControl.PipsPerChart = value;
    }

    partial void OnUnitsPerChartChanged(int value)
    {
        TickChartControl.UnitsPerChart = value;
    }

    partial void OnKernelShiftPercentChanged(double value)
    {
        TickChartControl.KernelShiftPercent = value;
    }

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        TickChartControl.Detach();
        TickChartControl = null!;
    }
}