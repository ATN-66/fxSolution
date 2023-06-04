/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                            DashboardViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Dashboard;
using Terminal.WinUI3.Services.Messenger.Messages;

namespace Terminal.WinUI3.ViewModels;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private ObservableCollection<GroupTitleList> _groupList;
    private readonly IDashboardService _dashboardService;
    private string _selectedDashboardItemId = null!;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        SelectedDashboardItemId = _dashboardService.SelectedDashboardItemId;
        _groupList = _dashboardService.GetGroupsWithItems();
    }

    public string SelectedDashboardItemId
    {
        get => _selectedDashboardItemId;
        set
        {
            if (_selectedDashboardItemId == value)
            {
                return;
            }

            _selectedDashboardItemId = value;
            _dashboardService.SelectedDashboardItemId = _selectedDashboardItemId;
        }
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }

    public void SendMessage(string id)
    {
        Messenger.Send(new DashboardChangedMessage(new DashboardMessage() { Id = id }));
    }
}