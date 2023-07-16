/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               SymbolViewModel.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
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
using System.Windows.Input;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account.Enums;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolViewModel : ObservableRecipient, INavigationAware
{
    private readonly IVisualService _visualService;
    private readonly IProcessor _processor;
    private readonly IAccountService _accountService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<SymbolViewModel> _logger;

    [ObservableProperty] private float _pipsPerChart = 100;// todo:settings
    [ObservableProperty] private float _maxPipsPerChart = 200;// todo:settings
    [ObservableProperty] private float _minPipsPerChart = 10;// todo:settings
    [ObservableProperty] private int _unitsPerChart = 500;// todo:settings
    [ObservableProperty] private int _maxUnitsPerChart = int.MaxValue;
    [ObservableProperty] private int _minUnitsPerChart = 10;// todo:settings
    [ObservableProperty] private double _kernelShiftPercent = 100;

    private ICommand _operationalCommand = null!;
    private string _operationalButtonContent = null!;

    public SymbolViewModel(IVisualService visualService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService, ILogger<SymbolViewModel> logger)
    {
        _visualService = visualService;
        _processor = processor;
        _accountService = accountService;
        _accountService.PropertyChanged += OnAccountServicePropertyChanged;
        _dispatcherService = dispatcherService;
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

    public string UpCurrency
    {
        set => TickChartControl.UpCurrency = value;
    }

    public string DownCurrency
    {
        set => TickChartControl.DownCurrency = value;
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

        UpdateOperationalProperties();
    }

    public void OnNavigatedFrom()
    {
        TickChartControl.Detach();
        TickChartControl = null!;
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