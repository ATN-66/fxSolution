using System.Collections.ObjectModel;

namespace Terminal.WinUI3.Models.Dashboard;

public class NavigationItem
{
    public NavigationItem(string content, string tag, string glyph, string navigateTo, bool isPageToNavigate)
    {
        Content = content;
        Tag = tag;
        Glyph = glyph;
        NavigateTo = navigateTo;
        IsPageToNavigate = isPageToNavigate;
    }

    public string Content
    {
        get; set;
    }
    public string Tag
    {
        get; set;
    }
    public string Glyph
    {
        get; set;
    }
    public string NavigateTo
    {
        get; set;
    }
    public bool IsPageToNavigate
    {
        get; set;
    }

    public ObservableCollection<NavigationItem>? NavigationItems
    {
        get;
        set;
    } = null;
}
