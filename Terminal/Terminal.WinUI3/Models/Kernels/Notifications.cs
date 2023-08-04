/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 Notifications.cs |
  +------------------------------------------------------------------+*/

using System.Linq;
using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Models.Kernels;

public class Notifications : INotificationsKernel
{
    private readonly IList<NotificationBase> _items = new List<NotificationBase>();

    public void Add(NotificationBase notificationBase)
    {
        _items.Add(notificationBase);
    }

    public void DeSelectAll()
    {
        foreach (var item in _items)
        {
            item.IsSelected = false;
        }
    }

    public IEnumerable<NotificationBase> GetAllNotifications(Symbol symbol, ViewPort viewPort)
    {
        var dateTimeNotifications = _items.OfType<IDateTimeNotification>().
            Where(notification => notification.DateTime >= viewPort.Start && notification.DateTime <= viewPort.End).Cast<NotificationBase>(); ;
        var priceNotifications = _items.OfType<IPriceNotification>().
            Where(notification => notification.Price >= viewPort.Low && notification.Price <= viewPort.High).Cast<NotificationBase>(); ;

        return dateTimeNotifications.Concat(priceNotifications);
    }

    public IEnumerable<NotificationBase> GetAllNotifications(Symbol symbol)
    {
        return _items.Where(notification => notification.Symbol == symbol).ToList();
    }

    public List<CandlestickNotification> GetCandlestickNotifications(Symbol symbol, ViewPort viewPort)
    {
        return _items.OfType<CandlestickNotification>().Where(notification => notification.Symbol == symbol && notification.DateTime >= viewPort.Start && notification.DateTime <= viewPort.End).ToList();
    }

    public bool IsAnySelected(Symbol symbol)
    {
        return _items.Any(notification => notification.Symbol == symbol && notification.IsSelected);
    }

    public NotificationBase GetSelectedNotification(Symbol symbol)
    {
        var selectedNotifications = _items.Where(notification => notification.Symbol == symbol && notification.IsSelected).ToList();
        if (!selectedNotifications.Any())
        {
            throw new InvalidOperationException("No notification is selected for the given symbol.");
        }
        if (selectedNotifications.Count > 1)
        {
            throw new InvalidOperationException("More than one notification is selected for the given symbol.");
        }
        return selectedNotifications.Single();
    }
}