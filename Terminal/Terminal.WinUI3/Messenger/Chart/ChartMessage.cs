using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Messenger.Chart;

public class ChartMessage : ValueChangedMessage<ChartEvent>
{
    public ChartType ChartType { get; init; }
    public Symbol Symbol { get; init; }
    public bool IsReversed { get; init; }

    public double DoubleValue { get; init; }
    public int IntValue { get; init; }
    public NotificationBase? Notification { get; init; }

    public ChartMessage(ChartEvent chartEvent) : base(chartEvent)
    {
    }
}