namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardItem
{
    public DashboardItem(string uniqueId, string title, string subtitle, string imagePath, string imageIconPath, string badgeString, string description, string content)
    {
        UniqueId = uniqueId;
        Title = title;
        Subtitle = subtitle;
        ImagePath = imagePath;
        ImageIconPath = imageIconPath;
        BadgeString = badgeString;
        Description = description;
        Content = content;
    }

    public string UniqueId
    {
        get;
        set;
    }

    public string Title
    {
        get;
        set;
    }

    public string Subtitle
    {
        get;
        set;
    }

    public string Description
    {
        get;
        set;
    }

    public string ImagePath
    {
        get;
        set;
    }

    public string ImageIconPath
    {
        get;
        set;
    }

    public string BadgeString
    {
        get;
        set;
    }

    public string Content
    {
        get;
        set;
    }

    public override string ToString() => Title;
}