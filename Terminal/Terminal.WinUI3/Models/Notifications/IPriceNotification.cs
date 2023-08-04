/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                            IPriceNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public interface IPriceNotification
{
    double Price { get; set; }
}