/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                             PriceNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public class PriceNotification : NotificationBase, IPriceNotification
{
    public double Price { get; set; }

    public override string ToString()
    {
        return $"PRICE notification: {Description}";
    }
}