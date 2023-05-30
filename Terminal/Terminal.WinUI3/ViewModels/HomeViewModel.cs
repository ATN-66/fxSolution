/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                                 HomeViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public partial class HomeViewModel : ObservableRecipient, INavigationAware
{
    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }
}