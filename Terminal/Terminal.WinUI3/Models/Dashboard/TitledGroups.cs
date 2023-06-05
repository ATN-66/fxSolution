/*+------------------------------------------------------------------+
  |                                 Terminal.WinUI3.Models.Dashboard |
  |                                                  TitledGroups.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Dashboard;

public class TitledGroups : List<DashboardItem>
{
    public TitledGroups(IEnumerable<DashboardItem> group) : base(group)
    {
    }

    public string Key
    {
        get;
        set;
    } = null!;

    public string Title
    {
        get;
        init;
    } = null!;

    public override string ToString() => Title;
}