using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class Root
{
    public ObservableCollection<DashboardGroup>? Groups
    {
        get; set;
    }
}