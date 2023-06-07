/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                                  JPYViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.ViewModels;

public partial class JPYViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "JPYViewModel_HeaderContext".GetLocalizedString();

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        // Run code when the app navigates away from this page
    }
}