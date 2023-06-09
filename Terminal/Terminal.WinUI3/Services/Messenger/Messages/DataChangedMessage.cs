using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Services.Messenger.Messages;

internal class DataChangedMessage : ValueChangedMessage<DailyContribution>
{
    public DataChangedMessage(DailyContribution value) : base(value)
    {

    }
}
