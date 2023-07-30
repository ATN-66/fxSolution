/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                             CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.ViewModels;

public class CurrencyViewModel : CurrencyViewModelBase
{
    public CurrencyViewModel(ISymbolOfCurrencyViewModelFactory symbolViewModelFactory, ILogger<CurrencyViewModelBase> logger) : base(symbolViewModelFactory, logger)
    {

    }
}