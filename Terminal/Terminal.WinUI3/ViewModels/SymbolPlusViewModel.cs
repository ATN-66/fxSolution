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
    private readonly IChartService _chartService;
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
        //DisposeChart();
        //ChartControlBaseFirst = _chartService.GetChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        //UpdateProperties(ChartControlBaseFirst);
        //UpdateOperationalProperties();
        //Currency = IsReversed ? ChartControlBaseFirst.BaseCurrency : ChartControlBaseFirst.QuoteCurrency;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CandlesticksAsync()
    {
        //DisposeChart();
        //ChartControlBaseFirst = _chartService.GetChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);
        //UpdateProperties();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ThresholdBarsAsync()
    {
        //DisposeChart();
        //ChartControlBaseFirst = _chartService.GetChart<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(Symbol, ChartType.ThresholdBar, IsReversed);
        //UpdateProperties();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearMessagesAsync()
    {
        ChartControlBaseFirst.ClearMessages();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ResetShiftsAsync()
    {
        ChartControlBaseFirst.HorizontalShift = HorizontalShift;
        ChartControlBaseFirst.ResetShifts();
        return Task.CompletedTask;
    }

    private void DisposeChart()
    {
        _chartService.DisposeChart(ChartControlBaseFirst);
        ChartControlBaseFirst.Detach();
        ChartControlBaseFirst = null!;
    }

    public SymbolPlusViewModel(IChartService chartService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService)
    {
        _chartService = chartService;
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

    private ChartControlBase _chartControlBaseFirst = null!;
    public ChartControlBase ChartControlBaseFirst
    {
        get => _chartControlBaseFirst;
        private set
        {
            if (_chartControlBaseFirst == value)
            {
                return;
            }

            _chartControlBaseFirst = value;
            OnPropertyChanged();
        }
    }

    private ChartControlBase _chartControlBaseSecond = null!;
    public ChartControlBase ChartControlBaseSecond
    {
        get => _chartControlBaseSecond;
        private set
        {
            if (_chartControlBaseSecond == value)
            {
                return;
            }

            _chartControlBaseSecond = value;
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
        ChartControlBaseFirst.CenturiesPercent = value;
        ChartControlBaseSecond.CenturiesPercent = value;
    }

    partial void OnUnitsPercentChanged(int value)
    {
        ChartControlBaseFirst.UnitsPercent = value;
    }

    partial void OnKernelShiftPercentChanged(int value)
    {
        ChartControlBaseFirst.KernelShiftPercent = value;
    }

    private void UpdateProperties(ChartControlBase chartControlBase)
    {
        chartControlBase.DataContext = this;
        chartControlBase.SetBinding(ChartControlBase.MinCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinCenturies)), Mode = BindingMode.OneWay });
        chartControlBase.SetBinding(ChartControlBase.MaxCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxCenturies)), Mode = BindingMode.OneWay });
        chartControlBase.SetBinding(ChartControlBase.CenturiesPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(CenturiesPercent)), Mode = BindingMode.TwoWay });
        chartControlBase.SetBinding(ChartControlBase.UnitsPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPercent)), Mode = BindingMode.TwoWay });
        chartControlBase.SetBinding(ChartControlBase.MinUnitsProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnits)), Mode = BindingMode.OneWay });
        chartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
        chartControlBase.SetBinding(ChartControlBase.HorizontalShiftProperty, new Binding { Source = this, Path = new PropertyPath(nameof(HorizontalShift)), Mode = BindingMode.OneWay });
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

    public async void OnNavigatedTo(object parameter)
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

        ChartControlBaseFirst = await _chartService.GetChartByTypeAsync(Symbol, IsReversed, ChartType.Candlesticks).ConfigureAwait(true);
        UpdateProperties(ChartControlBaseFirst);
        ChartControlBaseSecond = await _chartService.GetChartByTypeAsync(Symbol, IsReversed, ChartType.ThresholdBar).ConfigureAwait(true);
        UpdateProperties(ChartControlBaseSecond);

        UpdateOperationalProperties();
        Currency = IsReversed ? ChartControlBaseFirst.BaseCurrency : ChartControlBaseFirst.QuoteCurrency;
    }

    public void OnNavigatedFrom()
    {
        throw new NotImplementedException("SymbolPlusViewModel:OnNavigatedFrom()");

        _chartService.DisposeChart(ChartControlBaseFirst);
        ChartControlBaseFirst.Detach();
        ChartControlBaseFirst = null!;
    }
}