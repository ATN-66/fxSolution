namespace Terminal.WinUI3.Models.Maintenance;

public sealed class Info : IEquatable<Info>
{
    public static readonly Info Ticks = new Info(1, nameof(Ticks));
    public static readonly Info Candlesticks = new Info(2, nameof(Candlesticks));

    public string Name
    {
        get; private set;
    }

    public int Id
    {
        get; private set;
    }

    private Info(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public bool Equals(Info other)
    {
        return Id == other.Id;
    }
}