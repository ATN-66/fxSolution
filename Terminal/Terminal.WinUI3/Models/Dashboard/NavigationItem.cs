namespace Terminal.WinUI3.Models.Dashboard;

public class NavigationItem
{
    public NavigationItem(string content, string tag, string glyph, string navigateTo, bool isMain)
    {
        Content = content;
        Tag = tag;
        Glyph = glyph;
        NavigateTo = navigateTo;
        IsMain = isMain;
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
    public bool IsMain
    {
        get; set;
    }
}
