namespace Terminal.WinUI3.Models.Dashboard;

public class MenuItem
{
    public MenuItem(string content, string tag, string glyph, string navigateTo)
    {
        Content = content;
        Tag = tag;
        Glyph = glyph;
        NavigateTo = navigateTo;
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
}