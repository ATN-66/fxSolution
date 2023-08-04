/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Contracts.Models|
  |                                          INotificationsKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Contracts.Models;

public interface INotificationsKernel : IKernel
{
    void Add(NotificationBase notificationBase);
    void DeSelectAll();
    IEnumerable<NotificationBase> GetAllNotifications(Symbol symbol, ViewPort viewPort);
    List<CandlestickNotification> GetCandlestickNotifications(Symbol symbol, ViewPort viewPort);
    bool IsAnySelected(Symbol symbol);
    NotificationBase GetSelectedNotification(Symbol symbol);
}