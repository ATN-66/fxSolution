/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                 ThresholdBarChartNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public class ThresholdBarChartNotification : NotificationBase, IDateTimeNotification
{
    public DateTime DateTime
    {
        get;
        set;
    }
}