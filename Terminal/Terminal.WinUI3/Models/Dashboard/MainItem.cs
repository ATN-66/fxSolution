/*+------------------------------------------------------------------+
  |                                 Terminal.WinUI3.Models.Dashboard |
  |                                                      MainItem.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Dashboard;

public class MainItem
{
    public MainItem(string content, string tag, string glyph, string navigateTo)
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