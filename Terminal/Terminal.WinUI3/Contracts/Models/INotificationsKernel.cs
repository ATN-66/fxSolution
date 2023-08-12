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
    List<NotificationBase> GetAllNotifications(Symbol symbol, ViewPort viewPort);
    NotificationBase? GetSelectedNotification(Symbol symbol, ViewPort viewPort);
    void DeleteSelected();
    void DeleteAll();
    (DateTime, DateTime) GetDateTimeRange();
}