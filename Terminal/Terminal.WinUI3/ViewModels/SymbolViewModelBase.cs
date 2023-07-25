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

public abstract partial class SymbolViewModelBase : ObservableRecipient, INavigationAware
{
    protected readonly IVisualService VisualService;
    private readonly IProcessor _processor;
    private readonly IAccountService _accountService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty] private double _pipsPerChart = 50;// todo:settings
    [ObservableProperty] private float _maxPipsPerChart = 200;// todo:settings
    [ObservableProperty] private float _minPipsPerChart = 10;// todo:settings
    [ObservableProperty] private int _unitsPerChart;
    [ObservableProperty] private int _maxUnitsPerChart;
    [ObservableProperty] private int _minUnitsPerChart = 10;// todo:settings
    [ObservableProperty] private double _kernelShiftPercent = 100;

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
        VisualService.DisposeChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);
        ChartControlBase.Detach();
        ChartControlBase = null!;

        ChartControlBase = VisualService.GetChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        UpdateProperties();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CandlesticksAsync()
    {
        VisualService.DisposeChart<TickChartControl, Quotation, QuotationKernel>(Symbol, ChartType.Ticks, IsReversed);
        ChartControlBase.Detach();
        ChartControlBase = null!;

        ChartControlBase = VisualService.GetChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);
        UpdateProperties();

        return Task.CompletedTask;
    }

    protected SymbolViewModelBase(IVisualService visualService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService)
    {
        VisualService = visualService;
        _processor = processor;
        _accountService = accountService;
        _accountService.PropertyChanged += OnAccountServicePropertyChanged;
        _dispatcherService = dispatcherService;
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

    partial void OnPipsPerChartChanged(double value)
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

    protected void UpdateProperties()
    {
        ChartControlBase.DataContext = this;
        ChartControlBase.SetBinding(ChartControlBase.PipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(PipsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxPipsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.MinPipsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinPipsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.UnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(UnitsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MaxUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MaxUnitsPerChart)), Mode = BindingMode.TwoWay });
        ChartControlBase.SetBinding(ChartControlBase.MinUnitsPerChartProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MinUnitsPerChart)), Mode = BindingMode.OneWay });
        ChartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });

        UpdateOperationalProperties();
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

    public abstract void OnNavigatedTo(object parameter);

    public abstract void OnNavigatedFrom();
}