/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               SymbolViewModel.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using Binding = Microsoft.UI.Xaml.Data.Binding;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.AI.Interfaces;
using System.Windows.Input;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account.Enums;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolViewModel : ObservableRecipient, INavigationAware
{
    private readonly IVisualService _visualService;
    private readonly IProcessor _processor;
    private readonly IAccountService _accountService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty] private float _pipsPerChart = 100;// todo:settings
    [ObservableProperty] private float _maxPipsPerChart = 200;// todo:settings
    [ObservableProperty] private float _minPipsPerChart = 10;// todo:settings
    [ObservableProperty] private int _unitsPerChart = 500;// todo:settings
    [ObservableProperty] private int _maxUnitsPerChart = int.MaxValue;
    [ObservableProperty] private int _minUnitsPerChart = 10;// todo:settings
    [ObservableProperty] private double _kernelShiftPercent = 100;

    private ICommand _operationalCommand = null!;
    private string _operationalButtonContent = null!;

    public SymbolViewModel(IVisualService visualService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService)
    {
        _visualService = visualService;
        _processor = processor;
        _accountService = accountService;
        _accountService.PropertyChanged += OnAccountServicePropertyChanged;
        _dispatcherService = dispatcherService;
    }

    public ChartControlBase ChartControlBase
    {
        get;
        set;
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

    public string UpCurrency
    {
        set => ChartControlBase.UpCurrency = value;
    }

    public string DownCurrency
    {
        set => ChartControlBase.DownCurrency = value;
    }

    partial void OnPipsPerChartChanged(float value)
    {
        ChartControlBase.PipsPerChart = value;
    }

    partial void OnUnitsPerChartChanged(int value)
    {
        ChartControlBase.UnitsPerChart = value;
    }

    partial void OnKernelShiftPercentChanged(double value)
    {
        ChartControlBase.KernelShiftPercent = value;
    }

    public void OnNavigatedTo(object parameter)
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

        //ChartControlBase = _visualService.GetChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        ChartControlBase = _visualService.GetChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);

        ChartControlBase.DataContext = this;
        ChartControlBase.SetBinding(ChartControlBase.PipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(PipsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxPipsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.MinPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinPipsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.UnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxUnitsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MinUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnitsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });

        (UpCurrency, DownCurrency) = GetCurrenciesFromSymbol(Symbol, IsReversed);
        UpdateOperationalProperties();
    }
    
    public void OnNavigatedFrom()
    {
        _visualService.DisposeChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }

    private static (string upCurrency, string downCurrency) GetCurrenciesFromSymbol(Symbol symbol, bool isReversed)
    {
        var symbolName = symbol.ToString();
        var firstCurrency = symbolName[..3];
        var secondCurrency = symbolName.Substring(3, 3);
        if (Enum.TryParse<Currency>(firstCurrency, out var firstCurrencyEnum) && Enum.TryParse<Currency>(secondCurrency, out var secondCurrencyEnum))
        {
            return isReversed ? (secondCurrencyEnum.ToString(), firstCurrencyEnum.ToString()) : (firstCurrencyEnum.ToString(), secondCurrencyEnum.ToString());
        }
        throw new Exception("Failed to parse currencies from symbol.");
    }

    private void OnAccountServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "ServiceState")
        {
            return;
        }

        UpdateOperationalProperties();
    }

    public ICommand OperationalCommand
    {
        get => _operationalCommand;
        set
        {
            _operationalCommand = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() => OnPropertyChanged());
        }
    }

    public string OperationalButtonContent
    {
        get => _operationalButtonContent;
        set
        {
            _operationalButtonContent = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() => OnPropertyChanged());
        }
    }

    private void UpdateOperationalProperties()
    {
        UpdateOperationalButtonContent();
        UpdateOperationalCommand();
    }

    private void UpdateOperationalCommand()
    {
        switch (_accountService.ServiceState)
        {
            case ServiceState.Off or ServiceState.Busy:
                OperationalCommand = new RelayCommand(execute: () => {}, canExecute: () => false);
                break;
            case ServiceState.ReadyToOpen:
            {
                async void ExecuteAsync()
                {
                    await _processor.OpenPositionAsync(Symbol, IsReversed).ConfigureAwait(true);
                }

                OperationalCommand = new RelayCommand(execute: ExecuteAsync, canExecute: () => true);
                break;
            }
            case ServiceState.ReadyToClose:
            {
                async void ExecuteAsync()
                {
                    await _processor.ClosePositionAsync(Symbol, IsReversed).ConfigureAwait(true);
                }

                OperationalCommand = new RelayCommand(execute: ExecuteAsync, canExecute: () => true);
                break;
            }
            default:
                throw new InvalidOperationException($"{nameof(_accountService.ServiceState)} is not implemented.");
        }
    }

    private void UpdateOperationalButtonContent()
    {
        OperationalButtonContent = _accountService.ServiceState.GetDescription();
    }
}