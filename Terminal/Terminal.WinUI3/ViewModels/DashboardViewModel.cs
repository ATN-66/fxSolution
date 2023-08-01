/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                            DashboardViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Messenger.DataService;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.ViewModels;

public partial class DashboardViewModel : ObservableRecipient
{
    [ObservableProperty] private ObservableCollection<TitledGroups> _groups;
    private readonly IDashboardService _dashboardService;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        _groups = _dashboardService.GetTitledGroups();
    }

    public string SelectedItem
    {
        get => _dashboardService.SelectedItem!;
        set
        {
            if (value == _dashboardService.SelectedItem)
            {
                return;
            }

            _dashboardService.SelectedItem = value;

            var selectedItem = Groups
                .SelectMany(group => group)
                .FirstOrDefault(item => item.Id == _dashboardService.SelectedItem);

            Messenger.Send(new DashboardChangedMessage(new DashboardMessage(selectedItem!)));
        }
    }
}