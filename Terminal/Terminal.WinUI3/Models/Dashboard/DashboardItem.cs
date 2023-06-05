using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI.Controls;

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardItem
{
    public DashboardItem(string id, string title, bool isEnabled)
    {
        Id = id;
        Title = title;
        IsEnabled = isEnabled;
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

    public bool IsEnabled
    {
        get;
        set;
    }

    public bool IsSelected
    {
        get;
        set;
    }

    public ObservableCollection<NavigationItem> NavigationItems
    {
        get;
        set;
    }

    public override string ToString() => Title;
}