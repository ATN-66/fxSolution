namespace Terminal.WinUI3.Services.Messenger.Messages;

public class InfoMessage : DataServiceMessage<string>
{
    public InfoMessage(string value) : base(value)
    {
    }
}