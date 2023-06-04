﻿namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardItem
{
    public DashboardItem(string id, string title)
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

    public override string ToString() => Title;
}