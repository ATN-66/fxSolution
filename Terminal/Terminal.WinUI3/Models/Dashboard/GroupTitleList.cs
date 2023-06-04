namespace Terminal.WinUI3.Models.Dashboard;

public class GroupTitleList : List<object>
{
    public GroupTitleList(IEnumerable<object> items) : base(items)
    {
    }

    public object Key
    {
        get;
        set;
    } = null!;

    public string Title
    {
        get;
        set;
    } = null!;

    public override string ToString() => Title;
}