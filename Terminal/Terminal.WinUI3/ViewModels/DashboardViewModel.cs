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
    [ObservableProperty] private ObservableCollection<TitledGroups> _groups;
    private readonly IDashboardService _dashboardService;
    private string? _selectedItem;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        _groups = _dashboardService.GetTitledGroups();
    }

    public string SelectedItem
    {
        get => _selectedItem!;
        set
        {
            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            _dashboardService.SelectedItem = _selectedItem;
            Messenger.Send(new DashboardChangedMessage(new DashboardMessage() { Id = _selectedItem }));
        }
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }
}