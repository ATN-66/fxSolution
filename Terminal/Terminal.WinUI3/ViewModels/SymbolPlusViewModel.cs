using System.ComponentModel;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account.Enums;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolPlusViewModel : ObservableRecipient, INavigationAware
{
    private readonly IVisualService _visualService;
    private readonly IProcessor _processor;
    private readonly IAccountService _accountService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty] private double _minCenturies = 0.2d; // todo:settings
    [ObservableProperty] private double _maxCenturies = 3.0d; // todo:settings
    [ObservableProperty] private int _centuriesPercent = 50; // todo:settings

    [ObservableProperty] private int _minUnits = 10;// todo:settings
    [ObservableProperty] private int _unitsPercent = 100; // todo:settings
    [ObservableProperty] private int _kernelShiftPercent = 100; //todo:settings
    [ObservableProperty] private int _horizontalShift = 3; //todo:settings

    private string _operationalButtonContent = null!;
    private ICommand _operationalCommand = null!;
    public ICommand OperationalCommand
    {
        get => _operationalCommand;
        set
        {
            _operationalCommand = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() => OnPropertyChanged());
        }
    }

    [RelayCommand]
    private Task TicksAsync()
    {
        DisposeChart();
        ChartControlBase = _visualService.GetChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        UpdateProperties();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CandlesticksAsync()
    {
        DisposeChart();
        ChartControlBase = _visualService.GetChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);
        UpdateProperties();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ThresholdBarsAsync()
    {
        DisposeChart();
        ChartControlBase = _visualService.GetChart<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(Symbol, ChartType.ThresholdBar, IsReversed);
        UpdateProperties();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearMessagesAsync()
    {
        ChartControlBase.ClearMessages();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ResetShiftsAsync()
    {
        ChartControlBase.HorizontalShift = HorizontalShift;
        ChartControlBase.ResetShifts();
        return Task.CompletedTask;
    }

    private void DisposeChart()
    {
        _visualService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }

    public SymbolPlusViewModel(IVisualService visualService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService)
    {
        _visualService = visualService;
        _processor = processor;
        _accountService = accountService;
        _accountService.PropertyChanged += OnAccountServicePropertyChanged;
        _dispatcherService = dispatcherService;
    }

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

    public string Currency
    {
        get;
        set;
    } = null!;

    private ChartControlBase _chartControlBase = null!;
    public ChartControlBase ChartControlBase
    {
        get => _chartControlBase;
        private set
        {
            if (_chartControlBase == value)
            {
                return;
            }

            _chartControlBase = value;
            OnPropertyChanged();
        }
    }

    private void OnAccountServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "ServiceState")
        {
            return;
        }

        UpdateOperationalProperties();
    }

    partial void OnCenturiesPercentChanged(int value)
    {
        ChartControlBase.CenturiesPercent = value;
    }

    partial void OnUnitsPercentChanged(int value)
    {
        ChartControlBase.UnitsPercent = value;
    }

    partial void OnKernelShiftPercentChanged(int value)
    {
        ChartControlBase.KernelShiftPercent = value;
    }

    private void UpdateProperties()
    {
        ChartControlBase.DataContext = this;
        ChartControlBase.SetBinding(ChartControlBase.MinCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinCenturies)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxCenturies)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.CenturiesPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(CenturiesPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.UnitsPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MinUnitsProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnits)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.HorizontalShiftProperty, new Binding { Source = this, Path = new PropertyPath(nameof(HorizontalShift)), Mode = BindingMode.OneWay });
        UpdateOperationalProperties();
        Currency = IsReversed ? ChartControlBase.BaseCurrency : ChartControlBase.QuoteCurrency;
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
                OperationalCommand = new RelayCommand(execute: () => { }, canExecute: () => false);
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

        ChartControlBase = _visualService.GetDefaultChart(Symbol, IsReversed);
        UpdateProperties();
    }

    public void OnNavigatedFrom()
    {
        _visualService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }
}