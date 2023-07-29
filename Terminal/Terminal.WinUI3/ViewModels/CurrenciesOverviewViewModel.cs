/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                   CurrenciesOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public partial class CurrenciesOverviewViewModel : ObservableRecipient, INavigationAware
{
    private List<Symbol> Symbols { get; } = new();
    private List<bool> IsReversed { get; } = new();
    public List<SymbolOfCurrencyViewModel> SymbolViewModels { get; set; } = new();
    private readonly ISymbolOfCurrencyViewModelFactory _symbolViewModelFactory;

    [ObservableProperty] private int _centuriesPercent = 50; // todo:settings
    [ObservableProperty] private int _unitsPercent = 100; // todo:settings
    [ObservableProperty] private int _kernelShiftPercent = 100; //todo:settings

    public CurrenciesOverviewViewModel(ISymbolOfCurrencyViewModelFactory symbolViewModelFactory)
    {
        _symbolViewModelFactory = symbolViewModelFactory;
    }

    partial void OnCenturiesPercentChanged(int value)
    {
    }

    partial void OnUnitsPercentChanged(int value)
    {
    }

    partial void OnKernelShiftPercentChanged(int value)
    {
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }
}