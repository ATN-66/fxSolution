/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               SymbolViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using Binding = Microsoft.UI.Xaml.Data.Binding;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.AI.Interfaces;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolViewModel : ObservableRecipient, INavigationAware
{
    private readonly IVisualService _visualService;
    private readonly IProcessor _processor;
    private readonly ILogger<SymbolViewModel> _logger;

    [ObservableProperty] private float _pipsPerChart = 100;// todo:settings
    [ObservableProperty] private float _maxPipsPerChart = 200;// todo:settings
    [ObservableProperty] private float _minPipsPerChart = 10;// todo:settings
    [ObservableProperty] private int _unitsPerChart = 500;// todo:settings
    [ObservableProperty] private int _maxUnitsPerChart = int.MaxValue;
    [ObservableProperty] private int _minUnitsPerChart = 10;// todo:settings
    [ObservableProperty] private double _kernelShiftPercent = 100;

    public SymbolViewModel(IVisualService visualService, IProcessor processor, ILogger<SymbolViewModel> logger)
    {
        _visualService = visualService;
        _processor = processor;
        _logger = logger;
    }

    public TickChartControl TickChartControl
    {
        get;
        private set;
    } = null!;

    private Symbol Symbol
    {
        get;
        set;
    }

    private bool IsReversed
    {
        get;
        set;
    }

    public Currency UpCurrency
    {
        get;
        set;
    }

    public Currency DownCurrency
    {
        get;
        set;
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
        try
        {
            var parts = parameter.ToString()?.Split(',');
            var symbolStr = parts?[0].Trim();
            if (Enum.TryParse<Symbol>(symbolStr, out var symbol))
            {
                Symbol = symbol;
            }
            else
            {
                throw new Exception($"Failed to parse '{symbolStr}' into a Symbol enum value.");
            }

            IsReversed = bool.Parse(parts?[1].Trim()!);

            TickChartControl = _visualService.GetTickChartControl(Symbol, IsReversed)!;
            TickChartControl.DataContext = this;

            TickChartControl.SetBinding(TickChartControl.PipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(PipsPerChart)), Mode = BindingMode.TwoWay });
            TickChartControl.SetBinding(TickChartControl.MaxPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxPipsPerChart)), Mode = BindingMode.OneWay });
            TickChartControl.SetBinding(TickChartControl.MinPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinPipsPerChart)), Mode = BindingMode.OneWay });
            TickChartControl.SetBinding(TickChartControl.UnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPerChart)), Mode = BindingMode.TwoWay });
            TickChartControl.SetBinding(TickChartControl.MaxUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxUnitsPerChart)), Mode = BindingMode.TwoWay });
            TickChartControl.SetBinding(TickChartControl.MinUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnitsPerChart)), Mode = BindingMode.OneWay });
            TickChartControl.SetBinding(TickChartControl.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }

        (UpCurrency, DownCurrency) = GetCurrenciesFromSymbol(Symbol, IsReversed);
    }

    public void OnNavigatedFrom()
    {
        TickChartControl.Detach();
        TickChartControl = null!;
    }

    [RelayCommand]
    private Task OpenPositionAsync()
    {
        return _processor.OpenPositionAsync(Symbol, IsReversed);
    }

    private static (Currency upCurrency, Currency downCurrency) GetCurrenciesFromSymbol(Symbol symbol, bool isReversed)
    {
        var symbolName = symbol.ToString();
        var firstCurrency = symbolName[..3];
        var secondCurrency = symbolName.Substring(3, 3);
        if (Enum.TryParse<Currency>(firstCurrency, out var firstCurrencyEnum) && Enum.TryParse<Currency>(secondCurrency, out var secondCurrencyEnum))
        {
            return isReversed ? (secondCurrencyEnum, firstCurrencyEnum) : (firstCurrencyEnum, secondCurrencyEnum);
        }
        throw new Exception("Failed to parse currencies from symbol.");
    }
}