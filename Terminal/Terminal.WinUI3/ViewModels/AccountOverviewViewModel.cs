/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                      AccountOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public class AccountOverviewViewModel : ObservableRecipient, INavigationAware
{
    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        // Run code when the app navigates away from this page
    }
}