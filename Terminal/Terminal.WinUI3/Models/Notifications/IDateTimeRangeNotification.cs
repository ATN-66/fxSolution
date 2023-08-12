/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                    IDateTimeRangeNotification.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Notifications;

public interface IDateTimeRangeNotification : IDateTimeNotification
{
    DateTime End { get; set; }
}