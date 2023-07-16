/*+------------------------------------------------------------------+
  |                                 Terminal.WinUI3.Models.Dashboard |
  |                                                 DashboardItem.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardItem
{
    public DashboardItem(string id, string title, bool isEnabled, bool isSelected, MainItem mainItem)
    {
        Id = id;
        Title = title;
        IsEnabled = isEnabled;
        IsSelected = isSelected;
        MainItem   = mainItem;
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

    public MainItem MainItem
    {

        get;
        set;
    }

    public ObservableCollection<NavigationItem>? NavigationItems
    {
        get;
        set;
    } = null;

    public override string ToString() => Title;
}