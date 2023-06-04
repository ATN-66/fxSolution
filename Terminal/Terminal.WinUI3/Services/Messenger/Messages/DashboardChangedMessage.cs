using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Services.Messenger.Messages;

public class DashboardChangedMessage : ValueChangedMessage<DashboardMessage>
{
    public DashboardChangedMessage(DashboardMessage value) : base(value)
    {
    }
}