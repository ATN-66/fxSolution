/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                         CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Chart;

namespace Terminal.WinUI3.ViewModels;

public partial class CurrencyViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISymbolViewModelFactory _symbolViewModelFactory;

    private List<Symbol> Symbols { get; } = new();
    private List<bool> IsReversed { get; } = new();
    
    public ObservableCollection<SymbolViewModel> SymbolViewModels { get; set; } = new();

    [ObservableProperty] private int _centuriesPercent;
    [ObservableProperty] private int _unitsPercent;
    [ObservableProperty] private int _kernelShiftPercent;

    public CurrencyViewModel(IConfiguration configuration, ISymbolViewModelFactory symbolViewModelFactory)
    {
        _symbolViewModelFactory = symbolViewModelFactory;

        _centuriesPercent = configuration.GetValue<int>($"{nameof(_centuriesPercent)}");
        _unitsPercent = configuration.GetValue<int>($"{nameof(_unitsPercent)}");
        _kernelShiftPercent = configuration.GetValue<int>($"{nameof(_kernelShiftPercent)}");
    }

    [RelayCommand]
    private async Task TicksAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].TicksAsync().ConfigureAwait(true);
        }

        IsTicksSelected = true;
        IsCandlesticksSelected = IsThresholdBarsSelected = false;
    }

    [RelayCommand]
    public async Task CandlesticksAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].CandlesticksAsync().ConfigureAwait(true);
        }

        IsCandlesticksSelected = true;
        IsTicksSelected = IsThresholdBarsSelected = false;
    }

    [RelayCommand]
    public async Task ThresholdBarsAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ThresholdBarsAsync().ConfigureAwait(true);
        }

        IsThresholdBarsSelected = true;
        IsTicksSelected = IsCandlesticksSelected = false;
    }

    [RelayCommand]
    public async Task ClearMessagesAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ClearMessagesAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    public async Task ResetShiftsAsync()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            await SymbolViewModels[i].ResetShiftsAsync().ConfigureAwait(true);
        }
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

    public void OnNavigatedTo(object parameter)
    {
        SymbolViewModels.Clear();
        var input = parameter.ToString();
        CheckInput(input!);

        var parts = input!.Split(',').Select(part => part.Trim()).ToArray();

        if (Enum.TryParse(parts[0], out Currency _))
        {
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
            SymbolViewModels.Add(symbolViewModel);
            IsCandlesticksSelected = true;//todo: load from config
            IsTicksSelected = IsThresholdBarsSelected = false;//todo: load from config
        }
    }

    public void OnNavigatedFrom()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            SymbolViewModels[i].OnNavigatedFrom();
        }
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
        protected set
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
        protected set
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
        protected set
        {
            _isThresholdBarsSelected = value;
            IsThresholdBarsEnabled = !_isThresholdBarsSelected;
            OnPropertyChanged();
        }
    }
}