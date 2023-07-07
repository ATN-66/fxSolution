/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                             CurrencyViewModel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public class CurrencyViewModel : ObservableRecipient, INavigationAware
{
    public Currency Currency
    {
        get;
        private set;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (Enum.TryParse<Currency>((string)parameter, out var currency))
        {
            Currency = currency;
        }
    }

    public void OnNavigatedFrom()
    {
    }
}