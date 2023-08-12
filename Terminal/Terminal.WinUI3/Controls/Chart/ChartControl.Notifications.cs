/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                    ChartControl.Notifications.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Common.Entities;
using Microsoft.Graphics.Canvas;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Controls.Chart;
public abstract partial class ChartControl<TItem, TDataSourceKernel> where TItem : IChartItem where TDataSourceKernel : IDataSourceKernel<TItem>
{
    protected INotificationsKernel Notifications
    {
        get;
    }

    private static bool IsPointOnLine(Point point, NotificationBase? notification, double allowableDistance = ProximityThresholdStatic)
    {
        var lineStart = notification.StartPoint;
        var lineEnd = notification.EndPoint;
        var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
        var distance = Math.Abs((lineEnd.X - lineStart.X) * (lineStart.Y - point.Y) - (lineStart.X - point.X) * (lineEnd.Y - lineStart.Y)) / lineLength;
        return distance <= allowableDistance;
    }

    protected static void DrawSquares(CanvasDrawingSession session, NotificationBase? notificationBase, Color color)
    {
        const float halfSquareSize = SquareSize / 2f;
        Vector2 firstPoint, secondPoint;

        // Check if the notificationBase is vertical or horizontal
        if (notificationBase.StartPoint.X.Equals(notificationBase.EndPoint.X)) // It's a vertical notificationBase
        {
            firstPoint = notificationBase.StartPoint.Y < notificationBase.EndPoint.Y ? notificationBase.StartPoint : notificationBase.EndPoint;
            secondPoint = notificationBase.StartPoint.Y >= notificationBase.EndPoint.Y ? notificationBase.StartPoint : notificationBase.EndPoint;
            session.FillRectangle(firstPoint.X - halfSquareSize, firstPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
            session.FillRectangle(secondPoint.X - halfSquareSize, secondPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
        }
        else // It's a horizontal notificationBase
        {
            firstPoint = notificationBase.StartPoint.X < notificationBase.EndPoint.X ? notificationBase.StartPoint : notificationBase.EndPoint;
            secondPoint = notificationBase.StartPoint.X >= notificationBase.EndPoint.X ? notificationBase.StartPoint : notificationBase.EndPoint;
            session.FillRectangle(firstPoint.X - halfSquareSize, firstPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
            session.FillRectangle(secondPoint.X - halfSquareSize, secondPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
        }
    }

    protected int GetIndex(float positionX, IEnumerable<Vector2> points)
    {
        var distances = points.Skip(HorizontalShift).Take(Math.Min(Units - HorizontalShift, DataSource.Count)).Select((vector, index) => new { Distance = Math.Abs(vector.X - positionX), Index = index }).ToList();
        var minDistanceTuple = distances.Aggregate((a, b) => a.Distance < b.Distance ? a : b);
        var index = minDistanceTuple.Index;
        return index;
    }

    protected abstract void MoveSelectedNotification(double deltaX, double deltaY);

    protected double GetPrice(float positionY)
    {
        if (IsReversed)
        {
            return ViewPort.Low + (ViewPort.High - ViewPort.Low) * (positionY / GraphHeight);
        }

        return ViewPort.Low + (ViewPort.High - ViewPort.Low) * (1 - positionY / GraphHeight);
    }

    protected float GetPositionY(double price)
    {
        if (IsReversed)
        {
            return (float)(((price - ViewPort.Low) / (ViewPort.High - ViewPort.Low)) * GraphHeight);
        }

        return (float)((1 - ((price - ViewPort.Low) / (ViewPort.High - ViewPort.Low))) * GraphHeight);
    }
}