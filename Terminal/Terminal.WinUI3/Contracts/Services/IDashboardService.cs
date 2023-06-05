using System.Collections.ObjectModel;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDashboardService
{
    string? SelectedItem { get; set; }
    Task InitializeAsync();
    ObservableCollection<TitledGroups> GetTitledGroups();
}