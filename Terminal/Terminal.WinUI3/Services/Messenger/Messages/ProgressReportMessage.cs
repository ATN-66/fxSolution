namespace Terminal.WinUI3.Services.Messenger.Messages;

public class ProgressReportMessage : DataServiceMessage<int>
{
    public ProgressReportMessage(int value) : base(value)
    {
    }
}