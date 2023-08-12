/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                       CandlestickNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public class CandlestickNotification : NotificationBase, IDateTimeNotification
{
    public DateTime Start
    {
        get;
        set;
    }

    public override string ToString()
    {
        return $"CS notification: {Description}";
    }
}