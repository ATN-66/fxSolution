/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                    IDateTimeRangeNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public interface IDateTimeRangeNotification
{
    DateTime Start { get; set; }
    DateTime End { get; set; }
}