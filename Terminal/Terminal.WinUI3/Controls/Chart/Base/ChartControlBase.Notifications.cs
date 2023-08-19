using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Controls.Chart.Base;
public abstract partial class ChartControlBase : Control
{
    public abstract void DeleteSelectedNotification();
    public abstract void DeleteAllNotifications();
    public abstract void RepeatSelectedNotification();
    public abstract void OnRepeatSelectedNotification(NotificationBase notificationBase);
    public abstract void SaveItems();
    public abstract void SaveTransitions();
}
