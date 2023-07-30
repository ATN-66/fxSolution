/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                         CurrencyViewModelBase.cs |
  +------------------------------------------------------------------+*/

using System.Text.RegularExpressions;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public abstract partial class CurrencyViewModelBase : ObservableRecipient, INavigationAware
{
    private readonly ISymbolOfCurrencyViewModelFactory _symbolViewModelFactory;
    private readonly ILogger<CurrencyViewModelBase> _logger;

    private List<Symbol> Symbols { get; } = new();
    private List<bool> IsReversed { get; } = new();
    
    public List<SymbolOfCurrencyViewModel> SymbolViewModels { get; set; } = new();

    [ObservableProperty] private int _centuriesPercent = 50; // todo:settings
    [ObservableProperty] private int _unitsPercent = 100; // todo:settings
    [ObservableProperty] private int _kernelShiftPercent = 100; //todo:settings

    protected CurrencyViewModelBase(ISymbolOfCurrencyViewModelFactory symbolViewModelFactory, ILogger<CurrencyViewModelBase> logger)
    {
        _symbolViewModelFactory = symbolViewModelFactory;
        _logger = logger;
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
        try
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
                symbolViewModel.LoadChart();
                SymbolViewModels.Add(symbolViewModel);
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
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
}