/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                             CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.ViewModels;

public class CurrencyViewModel : CurrencyViewModelBase
{
    public CurrencyViewModel(ISymbolOfCurrencyViewModelFactory symbolViewModelFactory) : base(symbolViewModelFactory)
    {

    }
}