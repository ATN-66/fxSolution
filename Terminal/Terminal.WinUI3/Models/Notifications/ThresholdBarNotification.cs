/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                      ThresholdBarNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public class ThresholdBarNotification : NotificationBase, IDateTimeRangeNotification
{
    public DateTime Start
    {
        get;
        set;
    }

    public DateTime End
    {
        get;
        set;
    }

    public override string ToString()
    {
        return $"TB notification: {Description}";
    }
}