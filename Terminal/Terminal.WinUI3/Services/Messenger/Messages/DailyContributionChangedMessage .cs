using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Services.Messenger.Messages;

public class DailyContributionChangedMessage : DataServiceMessage<DailyContribution>
{
    public DailyContributionChangedMessage(DailyContribution value) : base(value)
    {
    }
}