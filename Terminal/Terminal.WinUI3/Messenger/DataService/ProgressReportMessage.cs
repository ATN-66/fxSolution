namespace Terminal.WinUI3.Messenger.DataService;

public class ProgressReportMessage : DataServiceMessage<int>
{
    public ProgressReportMessage(int value) : base(value)
    {
    }
}