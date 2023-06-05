/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _headerContext = "Overview";
    [ObservableProperty] private ObservableCollection<Contribution> _items;

    public TicksOverviewViewModel(IDataService dataService)
    {
        var tempList = dataService.GetTicksContributions();
        _items = new ObservableCollection<Contribution>(tempList);
    }

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        // Run code when the app navigates away from this page
    }
}