using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardGroup
{
    public DashboardGroup(string uniqueId, string title, string subtitle, string imagePath, string imageIconPath, string description, string folder, bool isSpecialSection)
    {
        UniqueId = uniqueId;
        Title = title;
        Subtitle = subtitle;
        ImagePath = imagePath;
        ImageIconPath = imageIconPath;
        Description = description;
        Folder = folder;
        IsSpecialSection = isSpecialSection;
        Items = new ObservableCollection<DashboardItem>();
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

    public string Folder
    {
        get;
        set;
    }

    public bool IsSpecialSection
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