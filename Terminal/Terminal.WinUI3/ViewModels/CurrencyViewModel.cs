/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                             CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Controls.Chart.Base;
using Terminal.WinUI3.Messenger.Chart;
using Terminal.WinUI3.Models.Chart;
using Enum = System.Enum;

namespace Terminal.WinUI3.ViewModels;

public partial class CurrencyViewModel : ObservableRecipient, INavigationAware, IRecipient<ChartMessage>
{
    private readonly IChartService _chartService;
    private readonly ISymbolViewModelFactory _symbolViewModelFactory;

    private int _selectedIndex;
    private Currency _currency;
    private List<Symbol> Symbols { get; } = new();
    private List<bool> IsReversed { get; } = new();
    
    public ObservableCollection<SymbolViewModel> SymbolViewModels { get; set; } = new();

    [ObservableProperty] private int _centuriesPercent;
    [ObservableProperty] private int _unitsPercent;
    [ObservableProperty] private int _kernelShiftPercent;

    [ObservableProperty] private bool _isVerticalLineRequested;
    [ObservableProperty] private bool _isHorizontalLineRequested;
    [ObservableProperty] private bool _isVerticalLineRequestEnabled;
    [ObservableProperty] private bool _isHorizontalLineRequestEnabled;

    public CurrencyViewModel(IConfiguration configuration, IChartService chartService, ISymbolViewModelFactory symbolViewModelFactory)
    {
        _chartService = chartService;
        _symbolViewModelFactory = symbolViewModelFactory;

        _centuriesPercent = configuration.GetValue<int>($"{nameof(_centuriesPercent)}");
        _unitsPercent = configuration.GetValue<int>($"{nameof(_unitsPercent)}");
        _kernelShiftPercent = configuration.GetValue<int>($"_kernelShiftPercentDefault");
    }

    [RelayCommand(CanExecute = nameof(CanChangeChart))]
    private async Task TicksAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            if (!SymbolViewModels[i].IsSelected)
            {
                continue;
            }

            await SymbolViewModels[i].TicksAsync().ConfigureAwait(true);
            SetupCommands(ChartType.Ticks);
            return;
        }
    }

    [RelayCommand(CanExecute = nameof(CanChangeChart))]
    private async Task CandlesticksAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            if (!SymbolViewModels[i].IsSelected)
            {
                continue;
            }

            await SymbolViewModels[i].CandlesticksAsync().ConfigureAwait(true);
            SetupCommands(ChartType.Candlesticks);
            return;
        }
    }

    [RelayCommand(CanExecute = nameof(CanChangeChart))]
    private async Task ThresholdBarsAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            if (!SymbolViewModels[i].IsSelected)
            {
                continue;
            }

            await SymbolViewModels[i].ThresholdBarsAsync().ConfigureAwait(true);
            SetupCommands(ChartType.ThresholdBars);
            return;
        }
    }

    private bool CanChangeChart()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            if (SymbolViewModels[i].IsSelected)
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private async Task ClearMessagesAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ClearMessagesAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ResetShiftsAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ResetShiftsAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ResetVerticalShiftAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ResetHorizontalShiftAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ResetHorizontalShiftAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ResetVerticalShiftAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private Task DebugOneAsync()
    {
        _chartService.ListAllRegisteredCharts();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DebugTwoAsync()
    {
        _chartService.ListAllNonNullCharts();
        return Task.CompletedTask;
    }

    partial void OnCenturiesPercentChanged(int value)
    {
        foreach (var model in SymbolViewModels)
        {
            model.CenturiesPercent = value;
        }
    }

    partial void OnUnitsPercentChanged(int value)
    {
        foreach (var model in SymbolViewModels)
        {
            model.UnitsPercent = value;
        }
    }

    partial void OnKernelShiftPercentChanged(int value)
    {
        foreach (var model in SymbolViewModels)
        {
            model.KernelShiftPercent = value;
        }
    }

    partial void OnIsVerticalLineRequestedChanged(bool value)
    {
        SymbolViewModels[_selectedIndex].ChartControlBase.IsVerticalLineRequested = value;

        if (value && IsHorizontalLineRequested)
        {
            SymbolViewModels[_selectedIndex].IsHorizontalLineRequested = IsHorizontalLineRequested = false;
        }
    }

    partial void OnIsHorizontalLineRequestedChanged(bool value)
    {
        SymbolViewModels[_selectedIndex].ChartControlBase.IsHorizontalLineRequested = value;

        if (value && IsVerticalLineRequested)
        {
            SymbolViewModels[_selectedIndex].IsVerticalLineRequested = IsVerticalLineRequested = false;
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        SymbolViewModels.Clear();
        var input = parameter.ToString();
        CheckInput(input!);

        var parts = input!.Split(',').Select(part => part.Trim()).ToArray();

        if (Enum.TryParse(parts[0], out Currency currency))
        {
            _currency = currency;
        }
        else
        {
            throw new Exception($"Invalid currency: {parts[0]}");
        }

        for (var i = 1; i < parts.Length; i++)
        {
            var symbolParts = parts[i].Trim('(', ')').Split(':');
            if (Enum.TryParse(symbolParts[0], out Symbol symbol))
            {
                var isReversed = bool.Parse(symbolParts[1]);
                Symbols.Add(symbol);
                IsReversed.Add(isReversed);
            }
            else
            {
                throw new Exception($"Invalid symbol: {symbolParts[0]}");
            }
        }

        for (var i = 0; i < Symbols.Count; i++)
        {
            var symbolViewModel = _symbolViewModelFactory.Create();
            symbolViewModel.Symbol = Symbols[i];
            symbolViewModel.IsReversed = IsReversed[i];
            symbolViewModel.LoadChart(ChartType.Candlesticks);//todo: load from config
            symbolViewModel.CenturiesPercent = CenturiesPercent;
            symbolViewModel.UnitsPercent = UnitsPercent;
            symbolViewModel.KernelShiftPercent = KernelShiftPercent;
            SymbolViewModels.Add(symbolViewModel);
        }

        StrongReferenceMessenger.Default.Register(this, new CurrencyToken(_currency));
    }

    public void OnNavigatedFrom()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            SymbolViewModels[i].OnNavigatedFrom();
        }

        StrongReferenceMessenger.Default.UnregisterAll(this);
    }

    [GeneratedRegex(@"^[A-Z]{3}, \(\w+:[\w\d]+\)(, \(\w+:[\w\d]+\))*$")]
    private static partial Regex IsValidFormat();
    private static void CheckInput(string input)
    {
        if (!IsValidFormat().IsMatch(input))
        {
            throw new ArgumentException("Invalid argument format. Expected format: 'Currency, (Symbol1:Bool), (Symbol2:Bool), (Symbol3:Bool)'");
        }
    }

    private bool _isTicksEnabled;
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
        protected set
        {
            _isTicksSelected = value;
            IsTicksEnabled = !_isTicksSelected;
            OnPropertyChanged(); 
        }
    }

    private bool _isCandlesticksEnabled;
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

    private bool _isThresholdBarsEnabled;
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

    public void Receive(ChartMessage message)
    {
        switch (message.Value)
        {
            case ChartEvent.IsSelected: OnIsSelected(message); break;
            case ChartEvent.CenturyShift: OnCenturyShift(message); break;
            case ChartEvent.HorizontalShift: OnHorizontalShift(message); break;
            case ChartEvent.KernelShift: OnKernelShift(message); break;
            case ChartEvent.RepeatSelectedNotification: OnRepeatSelectedNotification(message); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void OnIsSelected(ChartMessage message)
    {
        if (SymbolViewModels[0].Symbol != message.Symbol) { SymbolViewModels[0].ChartControlBase.IsSelected = false; }
        if (SymbolViewModels[1].Symbol != message.Symbol) { SymbolViewModels[1].ChartControlBase.IsSelected = false; }
        if (SymbolViewModels[2].Symbol != message.Symbol) { SymbolViewModels[2].ChartControlBase.IsSelected = false; }

        SetupCommands(message.ChartType);
        IsVerticalLineRequestEnabled = IsHorizontalLineRequestEnabled = true;

        _selectedIndex = Symbols.IndexOf(message.Symbol);
        SymbolViewModels[_selectedIndex].ChartControlBase.SetBinding(ChartControlBase.KernelShiftPercentProperty, new Binding { Source = this, Path = new PropertyPath(nameof(KernelShiftPercent)), Mode = BindingMode.TwoWay });
        SymbolViewModels[_selectedIndex].ChartControlBase.SetBinding(ChartControlBase.IsVerticalLineRequestedProperty, new Binding { Source = this, Path = new PropertyPath(nameof(IsVerticalLineRequested)), Mode = BindingMode.TwoWay });
        SymbolViewModels[_selectedIndex].ChartControlBase.SetBinding(ChartControlBase.IsHorizontalLineRequestedProperty, new Binding { Source = this, Path = new PropertyPath(nameof(IsHorizontalLineRequested)), Mode = BindingMode.TwoWay });
    }

    private void OnCenturyShift(ChartMessage message)
    {
        if (SymbolViewModels[0].Symbol != message.Symbol) { SymbolViewModels[0].ChartControlBase.OnCenturyShift(message.IsReversed, message.DoubleValue); }
        if (SymbolViewModels[1].Symbol != message.Symbol) { SymbolViewModels[1].ChartControlBase.OnCenturyShift(message.IsReversed, message.DoubleValue); }
        if (SymbolViewModels[2].Symbol != message.Symbol) { SymbolViewModels[2].ChartControlBase.OnCenturyShift(message.IsReversed, message.DoubleValue); }
    }

    private void OnHorizontalShift(ChartMessage message)
    {
        if (SymbolViewModels[0].Symbol != message.Symbol) { SymbolViewModels[0].ChartControlBase.OnHorizontalShift(message.IntValue); }
        if (SymbolViewModels[1].Symbol != message.Symbol) { SymbolViewModels[1].ChartControlBase.OnHorizontalShift(message.IntValue); }
        if (SymbolViewModels[2].Symbol != message.Symbol) { SymbolViewModels[2].ChartControlBase.OnHorizontalShift(message.IntValue); }
    }

    private void OnKernelShift(ChartMessage message)
    {
        if (SymbolViewModels[0].Symbol != message.Symbol) { SymbolViewModels[0].ChartControlBase.OnKernelShift(message.IntValue); }
        if (SymbolViewModels[1].Symbol != message.Symbol) { SymbolViewModels[1].ChartControlBase.OnKernelShift(message.IntValue); }
        if (SymbolViewModels[2].Symbol != message.Symbol) { SymbolViewModels[2].ChartControlBase.OnKernelShift(message.IntValue); }
    }

    private void OnRepeatSelectedNotification(ChartMessage message)
    {
        if (SymbolViewModels[0].Symbol != message.Symbol) { SymbolViewModels[0].ChartControlBase.OnRepeatSelectedNotification(message.Notification); }
        if (SymbolViewModels[1].Symbol != message.Symbol) { SymbolViewModels[1].ChartControlBase.OnRepeatSelectedNotification(message.Notification); }
        if (SymbolViewModels[2].Symbol != message.Symbol) { SymbolViewModels[2].ChartControlBase.OnRepeatSelectedNotification(message.Notification); }
    }

    private void SetupCommands(ChartType chartType)
    {
        IsHorizontalLineRequested = IsVerticalLineRequested = false;
        IsVerticalLineRequestEnabled = IsHorizontalLineRequestEnabled = false;

        TicksCommand.NotifyCanExecuteChanged();
        CandlesticksCommand.NotifyCanExecuteChanged();
        ThresholdBarsCommand.NotifyCanExecuteChanged();

        switch (chartType)
        {
            case ChartType.Ticks:
                IsCandlesticksEnabled = IsThresholdBarsEnabled = IsTicksSelected = true;
                IsTicksEnabled = IsCandlesticksSelected = IsThresholdBarsSelected = false;
                break;
            case ChartType.Candlesticks:
                IsTicksEnabled  = IsThresholdBarsEnabled = IsCandlesticksSelected = true;
                IsCandlesticksEnabled = IsTicksSelected = IsThresholdBarsSelected = false;
                break;
            case ChartType.ThresholdBars:
                IsTicksEnabled = IsCandlesticksEnabled = IsThresholdBarsSelected = true;
                IsThresholdBarsEnabled = IsTicksSelected = IsCandlesticksSelected = false;
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}