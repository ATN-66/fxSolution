using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardGroup
{
    public DashboardGroup(string id, string title)
    {
        Id = id;
        Title = title;
        Items = new ObservableCollection<DashboardItem>();
    }

    public string Id
    {
        get;
        set;
    }

    public string Title
    {
        get;
        set;
    }

    public ObservableCollection<DashboardItem> Items
    {
        get;
        set;
    }

    public override string ToString() => Title;
}