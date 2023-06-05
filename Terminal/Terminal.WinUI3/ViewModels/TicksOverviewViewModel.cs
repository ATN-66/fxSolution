/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Helpers;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "Overview";

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        // Run code when the app navigates away from this page
    }
}