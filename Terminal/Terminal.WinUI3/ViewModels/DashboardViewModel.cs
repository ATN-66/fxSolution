/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                            DashboardViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.ViewModels;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private ObservableCollection<GroupTitleList> _groupList;

    public DashboardViewModel()
    {
        var dashboardService = App.GetService<IDashboardService>();
        _groupList = dashboardService.GetGroupsWithItems();
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }
}