/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                       CandlestickNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public class CandlestickNotification : NotificationBase, IDateTimeNotification
{
    public DateTime DateTime
    {
        get;
        set;
    }
}