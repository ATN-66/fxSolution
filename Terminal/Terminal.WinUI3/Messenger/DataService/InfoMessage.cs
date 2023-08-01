namespace Terminal.WinUI3.Messenger.DataService;

public class InfoMessage : DataServiceMessage<string>
{
    public InfoMessage(string value) : base(value)
    {
    }
}