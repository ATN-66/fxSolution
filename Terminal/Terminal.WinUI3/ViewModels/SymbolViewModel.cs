using System.ComponentModel;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account.Enums;
using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Controls.Chart.Candlestick;
using Terminal.WinUI3.Controls.Chart.ThresholdBar;
using Terminal.WinUI3.Controls.Chart.Tick;
using ICoordinator = Terminal.WinUI3.Contracts.Services.ICoordinator;
using Terminal.WinUI3.Models.Kernel;
using Terminal.WinUI3.Models.Entities;
using Terminal.WinUI3.Models.Chart;
using ChartControlBase = Terminal.WinUI3.Controls.Chart.Base.ChartControlBase;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolViewModel : ObservableRecipient, INavigationAware
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

    [RelayCommand(CanExecute = nameof(CanExecuteOperation))]
    private Task OperateAsync()
    {
        switch (_accountService.ServiceState)
        {
            case ServiceState.ReadyToOpen: _coordinator.DoOpenPositionAsync(Symbol, IsReversed); break;
            case ServiceState.ReadyToClose: _coordinator.DoClosePositionAsync(Symbol, IsReversed); break;
            case ServiceState.Off:
            case ServiceState.Busy: break;
            default: throw new InvalidOperationException($"{nameof(_accountService.ServiceState)} is not implemented.");
        }
        
        return Task.CompletedTask;
    }
    private bool CanExecuteOperation()
    {
        return _accountService.ServiceState is ServiceState.ReadyToOpen or ServiceState.ReadyToClose;
    }
    private void OnAccountServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateOperationalProperties();
        OperateCommand.NotifyCanExecuteChanged();
    }
    private void UpdateOperationalProperties()
    {
        OperationalContent = _accountService.ServiceState.GetDescription();
        Currency = IsReversed ? ChartControlBase.BaseCurrency : ChartControlBase.QuoteCurrency;
    }

    [RelayCommand]
    public async Task TicksAsync()
    {
        DisposeChart();
        ChartControlBase = await _chartService.GetChartAsync<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed).ConfigureAwait(true);
        SetChartBindings();

        IsTicksSelected = true;
        IsCandlesticksSelected = IsThresholdBarsSelected = false;
    }

    [RelayCommand]
    public async Task CandlesticksAsync()
    {
        DisposeChart();
        ChartControlBase = await _chartService.GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed).ConfigureAwait(true);
        SetChartBindings();

        IsCandlesticksSelected = true;
        IsTicksSelected = IsThresholdBarsSelected = false;
    }

    [RelayCommand]
    public async Task ThresholdBarsAsync()
    {
        DisposeChart();
        ChartControlBase = await _chartService.GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(Symbol, ChartType.ThresholdBars, IsReversed).ConfigureAwait(true);
        SetChartBindings();

        IsThresholdBarsSelected = true;
        IsTicksSelected = IsCandlesticksSelected = false;
    }

    [RelayCommand]
    public Task ClearMessagesAsync()
    {
        ChartControlBase.ClearMessages();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ResetShiftsAsync()
    {
        ChartControlBase.HorizontalShift = HorizontalShift;
        ChartControlBase.ResetShifts();
        return Task.CompletedTask;
    }

    private void DisposeChart()
    {
        _chartService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }

    public SymbolViewModel(IConfiguration configuration, IChartService chartService, ICoordinator coordinator, IAccountService accountService, IDispatcherService dispatcherService)
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

    private void SetChartBindings()
    {
        ChartControlBase.DataContext = this;
        ChartControlBase.SetBinding(ChartControlBase.MinCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinCenturies)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxCenturiesProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxCenturies)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.CenturiesPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(CenturiesPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.UnitsPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MinUnitsProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnits)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.HorizontalShiftProperty, new Binding { Source = this, Path = new PropertyPath(nameof(HorizontalShift)), Mode = BindingMode.OneWay });
    }

    private string _operationalButtonContent = null!;
    public string OperationalContent
    {
        get => _operationalButtonContent;
        private set
        {
            _operationalButtonContent = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() => OnPropertyChanged());
        }
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

        ChartControlBase = await _chartService.GetDefaultChartAsync(Symbol, IsReversed).ConfigureAwait(true);

        var type = ChartControlBase.GetType();
        switch (type)
        {
            case var _ when type == typeof(TickChartControl):
                IsTicksSelected = true;
                IsCandlesticksSelected = IsThresholdBarsSelected = false;
                break;
            case var _ when type == typeof(CandlestickChartControl):
                IsCandlesticksSelected = true;
                IsTicksSelected = IsThresholdBarsSelected = false;
                break;
            case var _ when type == typeof(ThresholdBarChartControl):
                IsThresholdBarsSelected = true;
                IsTicksSelected = IsCandlesticksSelected = false;
                break;
            default: throw new Exception($"Unknown chart type: {type}");
        }

        SetChartBindings();
        UpdateOperationalProperties();
    }

    public void OnNavigatedFrom()
    {
        _chartService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
        _accountService.PropertyChanged -= OnAccountServicePropertyChanged;
    }

    public async void LoadChart(ChartType chartType)
    {
        ChartControlBase = chartType switch
        {
            ChartType.Ticks => await _chartService.GetChartAsync<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed).ConfigureAwait(false),
            ChartType.Candlesticks => await _chartService.GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed).ConfigureAwait(false),
            ChartType.ThresholdBars => await _chartService.GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(Symbol, ChartType.ThresholdBars, IsReversed).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(chartType), chartType, null)
        };

        SetChartBindings();
        UpdateOperationalProperties();
    }

    private bool _isTicksEnabled = true;
    public bool IsTicksEnabled
    {
        get => _isTicksEnabled;
        private set
        {
            _isTicksEnabled = value;
            OnPropertyChanged();
        }
    }
    private bool _isTicksSelected;
    public bool IsTicksSelected
    {
        get => _isTicksSelected;
        private set
        {
            _isTicksSelected = value;
            IsTicksEnabled = !_isTicksSelected;
            OnPropertyChanged();
        }
    }

    private bool _isCandlesticksEnabled = true;
    public bool IsCandlesticksEnabled
    {
        get => _isCandlesticksEnabled;
        private set
        {
            _isCandlesticksEnabled = value;
            OnPropertyChanged();
        }
    }
    private bool _isCandlesticksSelected;
    public bool IsCandlesticksSelected
    {
        get => _isCandlesticksSelected;
        private set
        {
            _isCandlesticksSelected = value;
            IsCandlesticksEnabled = !_isCandlesticksSelected;
            OnPropertyChanged();
        }
    }

    private bool _isThresholdBarsEnabled = true;
    public bool IsThresholdBarsEnabled
    {
        get => _isThresholdBarsEnabled;
        private set
        {
            _isThresholdBarsEnabled = value;
            OnPropertyChanged();
        }
    }
    private bool _isThresholdBarsSelected;
    public bool IsThresholdBarsSelected
    {
        get => _isThresholdBarsSelected;
        private set
        {
            _isThresholdBarsSelected = value;
            IsThresholdBarsEnabled = !_isThresholdBarsSelected;
            OnPropertyChanged();
        }
    }
}