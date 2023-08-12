/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                           SymbolPlusViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using Terminal.WinUI3.Models.Chart;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Controls.Chart.Base;
using Terminal.WinUI3.Messenger.Chart;

namespace Terminal.WinUI3.ViewModels;

public partial class SymbolPlusViewModel : ObservableRecipient, INavigationAware, IRecipient<ChartMessage>
{
    private readonly ISymbolViewModelFactory _symbolViewModelFactory;
    public ObservableCollection<SymbolViewModel> SymbolViewModels { get; } = new();
    private int? _selectedChart;
    private readonly List<ChartType> _charts = new();
    private Symbol Symbol { get; set; }
    private bool IsReversed { get; set; }
    private CommunicationToken _communicationToken = null!;

    [ObservableProperty] private int _centuriesPercent;
    [ObservableProperty] private int _unitsPercent;
    [ObservableProperty] private bool _isVerticalLineRequested;
    [ObservableProperty] private bool _isHorizontalLineRequested;
    [ObservableProperty] private bool _isVerticalLineRequestEnabled;
    [ObservableProperty] private bool _isHorizontalLineRequestEnabled;

    public SymbolPlusViewModel(IConfiguration configuration, ISymbolViewModelFactory symbolViewModelFactory)
    {
        _symbolViewModelFactory = symbolViewModelFactory;

        _centuriesPercent = configuration.GetValue<int>($"{nameof(_centuriesPercent)}");
        _unitsPercent = configuration.GetValue<int>($"{nameof(_unitsPercent)}");
        _charts.Add(ChartType.ThresholdBars);
        _charts.Add(ChartType.Candlesticks);
    }

    [RelayCommand]
    private async Task ClearMessagesAsync()
    {
        await SymbolViewModels[0].ClearMessagesAsync().ConfigureAwait(true);
        await SymbolViewModels[1].ClearMessagesAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ResetShiftsAsync()
    {
        await SymbolViewModels[0].ResetShiftsAsync().ConfigureAwait(true);
        await SymbolViewModels[1].ResetShiftsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ResetVerticalShiftAsync()
    {
        await SymbolViewModels[0].ResetHorizontalShiftAsync().ConfigureAwait(true);
        await SymbolViewModels[1].ResetHorizontalShiftAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ResetHorizontalShiftAsync()
    {
        await SymbolViewModels[0].ResetVerticalShiftAsync().ConfigureAwait(true);
        await SymbolViewModels[1].ResetVerticalShiftAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private Task DebugOneAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DebugTwoAsync()
    {
        return Task.CompletedTask;
    }
   
    partial void OnCenturiesPercentChanged(int value)
    {
        SymbolViewModels[0].CenturiesPercent = value;
        SymbolViewModels[1].CenturiesPercent = value;
    }
    partial void OnUnitsPercentChanged(int value)
    {
        SymbolViewModels[0].UnitsPercent = value;
        SymbolViewModels[1].UnitsPercent = value;
    }
    partial void OnIsVerticalLineRequestedChanged(bool value)
    {
        if (_selectedChart is null)
        {
            return;
        }

        SymbolViewModels[_selectedChart.Value].ChartControlBase.IsVerticalLineRequested = value;

        if (value && IsHorizontalLineRequested)
        {
            SymbolViewModels[_selectedChart.Value].IsHorizontalLineRequested = IsHorizontalLineRequested = false;
        }
    }
    partial void OnIsHorizontalLineRequestedChanged(bool value)
    {
        if (_selectedChart is null)
        {
            return;
        }

        SymbolViewModels[_selectedChart.Value].ChartControlBase.IsHorizontalLineRequested = value;

        if (value && IsVerticalLineRequested)
        {
            SymbolViewModels[_selectedChart.Value].IsVerticalLineRequested = IsVerticalLineRequested = false;
        }
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

        _communicationToken = new SymbolToken(Symbol);
        IsReversed = bool.Parse(parts?[1].Trim()!);

        for (var i = 0; i < _charts.Count; i++)
        {
            var symbolViewModel = _symbolViewModelFactory.Create();
            symbolViewModel.Symbol = Symbol;
            symbolViewModel.IsReversed = IsReversed;
            symbolViewModel.LoadChart(_charts[i]);
            symbolViewModel.CenturiesPercent = CenturiesPercent;
            symbolViewModel.UnitsPercent = UnitsPercent;
            SymbolViewModels.Add(symbolViewModel);
            symbolViewModel.CommunicationToken = symbolViewModel.ChartControlBase.CommunicationToken = _communicationToken;
        }

        StrongReferenceMessenger.Default.Register<SymbolPlusViewModel, ChartMessage, CommunicationToken>(recipient: this, token: _communicationToken, handler: (recipient, message) => recipient.Receive(message));
    }
    public void OnNavigatedFrom()
    {
        SymbolViewModels[0].OnNavigatedFrom();
        SymbolViewModels[1].OnNavigatedFrom();

        StrongReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(ChartMessage message)
    {
        switch (message.Value)
        {
            case ChartEvent.IsSelected: OnIsSelected(message); break;
            case ChartEvent.CenturyShift: OnCenturyShift(message); break;
            case ChartEvent.HorizontalShift: break;
            case ChartEvent.KernelShift: break;
            //case ChartEvent.RepeatSelectedNotification: OnRepeatSelectedNotification(message); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void OnIsSelected(ChartMessage message)
    {
        if (SymbolViewModels[0].ChartControlBase.ChartType != message.ChartType) { SymbolViewModels[0].ChartControlBase.IsSelected = false; }
        if (SymbolViewModels[1].ChartControlBase.ChartType != message.ChartType) { SymbolViewModels[1].ChartControlBase.IsSelected = false; }

        IsHorizontalLineRequested = IsVerticalLineRequested = false;
        IsVerticalLineRequestEnabled = IsHorizontalLineRequestEnabled = true;
       
        _selectedChart = _charts.IndexOf(message.ChartType);
        SymbolViewModels[_selectedChart.Value].ChartControlBase.SetBinding(ChartControlBase.IsVerticalLineRequestedProperty, new Binding { Source = this, Path = new PropertyPath(nameof(IsVerticalLineRequested)), Mode = BindingMode.TwoWay });
        SymbolViewModels[_selectedChart.Value].ChartControlBase.SetBinding(ChartControlBase.IsHorizontalLineRequestedProperty, new Binding { Source = this, Path = new PropertyPath(nameof(IsHorizontalLineRequested)), Mode = BindingMode.TwoWay });
    }

    private void OnCenturyShift(ChartMessage message)
    {
        if (SymbolViewModels[0].ChartControlBase.ChartType != message.ChartType) { SymbolViewModels[0].ChartControlBase.OnCenturyShift(message.IsReversed, message.DoubleValue); }
        if (SymbolViewModels[1].ChartControlBase.ChartType != message.ChartType) { SymbolViewModels[1].ChartControlBase.OnCenturyShift(message.IsReversed, message.DoubleValue); }
    }
}