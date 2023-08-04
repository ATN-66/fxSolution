using System.ComponentModel;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account.Enums;
using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Services;
using ICoordinator = Terminal.WinUI3.Contracts.Services.ICoordinator;
using Terminal.WinUI3.Models.Chart;
using ChartControlBase = Terminal.WinUI3.Controls.Chart.Base.ChartControlBase;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolPlusViewModel : ObservableRecipient, INavigationAware
{
    private readonly IChartService _chartService;
    private readonly ICoordinator _coordinator;
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
    private Task TicksAsync()
    {
        //DisposeChart();
        //ChartControlBase = await ChartService.GetChartAsync<TickChartControl, Quotation, Quotations>(Symbol, ChartType.Ticks, IsReversed).ConfigureAwait(true);
        //SetChartBindings();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CandlesticksAsync()
    {
        //DisposeChart();
        //ChartControlBaseFirst = _chartService.GetChart<CandlestickChartControl, Candlestick, DataSource>(Symbol, ChartType.DataSource, IsReversed);
        //SetChartBindings();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ThresholdBarsAsync()
    {
        //DisposeChart();
        //ChartControlBaseFirst = _chartService.GetChart<ThresholdBarChartControl, ThresholdBars, ThresholdBars>(Symbol, ChartType.ThresholdBars, IsReversed);
        //SetChartBindings();
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

    public SymbolPlusViewModel(IConfiguration configuration, IChartService chartService, ICoordinator coordinator, IAccountService accountService, IDispatcherService dispatcherService)
    {
        _chartService = chartService;
        _coordinator = coordinator;
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

    public Currency Currency
    {
        get;
        set;
    }

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
                //async void ExecuteAsync()
                //{
                //    //await _coordinator.DoOpenPositionAsync(Symbol, IsReversed).ConfigureAwait(true);
                //}

                //OperationalCommand = new RelayCommand(execute: ExecuteAsync, canExecute: () => true);
                break;
            }
            case ServiceState.ReadyToClose:
            {
                //async void ExecuteAsync()
                //{
                //    await _coordinator.DoClosePositionAsync(Symbol, IsReversed).ConfigureAwait(true);
                //}

                //OperationalCommand = new RelayCommand(execute: ExecuteAsync, canExecute: () => true);
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
        ChartControlBaseSecond = await _chartService.GetChartByTypeAsync(Symbol, IsReversed, ChartType.ThresholdBars).ConfigureAwait(true);
        UpdateProperties(ChartControlBaseSecond);

        UpdateOperationalProperties();
        Currency = IsReversed ? ChartControlBaseFirst.BaseCurrency : ChartControlBaseFirst.QuoteCurrency;
    }

    public void OnNavigatedFrom()
    {
        _chartService.DisposeChart(ChartControlBaseFirst);
        ChartControlBaseFirst.Detach();
        ChartControlBaseFirst = null!;

        _chartService.DisposeChart(ChartControlBaseSecond);
        ChartControlBaseSecond.Detach();
        ChartControlBaseSecond = null!;
    }
}