/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 Notifications.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Models.Kernels;

public class Notifications : INotificationsKernel
{
    public Symbol Symbol { get; }
    public ChartType ChartType { get; }

    private readonly IList<NotificationBase> _items = new List<NotificationBase>();

    public Notifications(Symbol symbol, ChartType chartType)
    {
        Symbol = symbol;
        ChartType = chartType;
    }

    public void Add(NotificationBase notificationBase)
    {
        if (notificationBase.IsSelected)
        {
            DeSelectAll();
        }

        _items.Add(notificationBase);
    }

    public void DeSelectAll()
    {
        foreach (var item in _items)
        {
            item.IsSelected = false;
        }
    }

    public List<NotificationBase> GetAllNotifications(Symbol symbol, ViewPort viewPort)
    {
        var dateTimeNotifications = _items.OfType<IDateTimeNotification>().
            Where(notification => notification.Start >= viewPort.Start && notification.Start <= viewPort.End).Cast<NotificationBase>(); 
        var priceNotifications = _items.OfType<IPriceNotification>().
            Where(notification => notification.Price >= viewPort.Low && notification.Price <= viewPort.High).Cast<NotificationBase>(); 
        return dateTimeNotifications.Concat(priceNotifications).ToList();
    }

    public NotificationBase? GetSelectedNotification(Symbol symbol, ViewPort viewPort)
    {
        var allNotifications = GetAllNotifications(symbol, viewPort);
        var selectedNotifications = allNotifications.Where(notification => notification.IsSelected).ToList();
        if (!selectedNotifications.Any())
        {
            return null;
        }

        if (selectedNotifications.Count > 1)
        {
            throw new InvalidOperationException("More than one notification is selected for the given symbol within the viewport.");
        }

        return selectedNotifications.Single();
    }

    public void DeleteSelected()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i].IsSelected)
            {
                _items.RemoveAt(i);
            }
        }
    }

    public void DeleteAll()
    {
        _items.Clear();
    }

    public (DateTime, DateTime) GetDateTimeRange() 
    {
        var dateTimeNotifications = _items.OfType<IDateTimeNotification>().Take(2).ToList();
        if (dateTimeNotifications.Count < 2)
        {
            throw new InvalidOperationException("There must be at least two items that implement IDateTimeNotification.");
        }

        var startDate = dateTimeNotifications[0].Start;
        DateTime endDate;
        if (dateTimeNotifications[1] is IDateTimeRangeNotification dateTimeRangeNotification)
        {
            endDate = dateTimeRangeNotification.End;
        }
        else
        {
            endDate = dateTimeNotifications[1].Start;
        }
        return (startDate, endDate);
    }
}