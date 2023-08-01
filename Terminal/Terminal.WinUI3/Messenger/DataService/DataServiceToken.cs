namespace Terminal.WinUI3.Messenger.DataService;

public sealed class DataServiceToken : IEquatable<DataServiceToken>
{
    public static readonly DataServiceToken DataToUpdate = new(1, nameof(DataToUpdate));
    public static readonly DataServiceToken Progress = new(2, nameof(Progress));
    public static readonly DataServiceToken Info = new(3, nameof(Info));

    public string Name
    {
        get; private set;
    }

    public int Id
    {
        get; private set;
    }

    private DataServiceToken(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public bool Equals(DataServiceToken? other)
    {
        return Id == other!.Id;
    }
}