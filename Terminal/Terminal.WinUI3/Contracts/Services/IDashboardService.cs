using System.Collections.ObjectModel;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDashboardService
{
    Task InitializeAsync();
    ObservableCollection<GroupTitleList> GetGroupsWithItems();
}