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
using Microsoft.Extensions.Configuration;

namespace Terminal.WinUI3.ViewModels;

public abstract partial class SymbolViewModelBase : ObservableRecipient, INavigationAware
{
    protected readonly IChartService ChartService;
    private readonly IProcessor _processor;
    private readonly IAccountService _accountService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty] private double _minCenturies;
    [ObservableProperty] private double _maxCenturies; 
    [ObservableProperty] private int _centuriesPercent;

    [ObservableProperty] private int _minUnits;
    [ObservableProperty] private int _unitsPercent;
    [ObservableProperty] private int _kernelShiftPercent;
    [ObservableProperty] private int _horizontalShift;

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
    private async Task TicksAsync()
    {
        DisposeChart();
        ChartControlBase = await ChartService.GetChartAsync<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed).ConfigureAwait(true);
        UpdateProperties();
    }

    [RelayCommand]
    private async Task CandlesticksAsync()
    {
        DisposeChart();
        ChartControlBase = await ChartService.GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed).ConfigureAwait(true);
        UpdateProperties();
    }

    [RelayCommand]
    private async Task ThresholdBarsAsync()
    {
        DisposeChart();
        ChartControlBase = await ChartService.GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(Symbol, ChartType.ThresholdBar, IsReversed).ConfigureAwait(true);
        UpdateProperties();
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
        ChartService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }

    protected SymbolViewModelBase(IConfiguration configuration, IChartService chartService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService)
    {
        ChartService = chartService;
        _processor = processor;
        _accountService = accountService;
        _accountService.PropertyChanged += OnAccountServicePropertyChanged;
        _dispatcherService = dispatcherService;

        _minCenturies = configuration.GetValue<double>($"{nameof(_minCenturies)}");
        _maxCenturies = configuration.GetValue<double>($"{nameof(_maxCenturies)}");
        _centuriesPercent = configuration.GetValue<int>($"{nameof(_centuriesPercent)}");
        _minUnits = configuration.GetValue<int>($"{nameof(_minUnits)}");
        _unitsPercent = configuration.GetValue<int>($"{nameof(_unitsPercent)}");
        _kernelShiftPercent = configuration.GetValue<int>($"{nameof(_kernelShiftPercent)}");
        _horizontalShift = configuration.GetValue<int>($"{nameof(_horizontalShift)}");
    }

    public Symbol Symbol
    {
        get;
        set;
    }

    public bool IsReversed
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
        protected set
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

    protected void UpdateProperties()
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
            default: throw new InvalidOperationException($"{nameof(_accountService.ServiceState)} is not implemented.");
        }
    }

    private void UpdateOperationalButtonContent()
    {
        OperationalButtonContent = _accountService.ServiceState.GetDescription();
    }

    public abstract void OnNavigatedTo(object parameter);

    public abstract void OnNavigatedFrom();
}