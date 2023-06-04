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

    public override string ToString() => Title;
}