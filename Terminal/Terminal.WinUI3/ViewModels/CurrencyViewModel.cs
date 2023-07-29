/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                             CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public partial class CurrencyViewModel : ObservableRecipient, INavigationAware
{
    private Currency Currency { get; set; }
    private List<Symbol> Symbols { get; } = new();
    private List<bool> IsReversed { get; } = new();
    private readonly ISymbolOfCurrencyViewModelFactory _symbolViewModelFactory;
    public List<SymbolOfCurrencyViewModel> SymbolViewModels { get; set; } = new();

    [ObservableProperty] private int _centuriesPercent = 50; // todo:settings
    [ObservableProperty] private int _unitsPercent = 100; // todo:settings
    [ObservableProperty] private int _kernelShiftPercent = 100; //todo:settings

    public CurrencyViewModel(ISymbolOfCurrencyViewModelFactory symbolViewModelFactory)
    {
        _symbolViewModelFactory = symbolViewModelFactory;
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
        var parts = input!.Split(',').Select(part => part.Trim()).ToArray();

        if (Enum.TryParse(parts[0], out Currency currency))
        {
            Currency = currency;
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
            symbolViewModel.LoadChart();
            SymbolViewModels.Add(symbolViewModel);
        }
    }

    public void OnNavigatedFrom()
    {
        for (var i = 0; i < Symbols.Count; i++)
        {
            SymbolViewModels[i].OnNavigatedFrom();
        }
    }
}