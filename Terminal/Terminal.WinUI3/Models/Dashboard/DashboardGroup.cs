/*+------------------------------------------------------------------+
  |                                 Terminal.WinUI3.Models.Dashboard |
  |                                                DashboardGroup.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardGroup
{
    public DashboardGroup(string id, string title)
    {
        Id = id;
        Title = title;
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
    } = new();

    public override string ToString() => Title;
}