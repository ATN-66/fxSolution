/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Common.Entities;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Messenger.AccountService;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Notifications;
using Terminal.WinUI3.Models.Trade;
using Terminal.WinUI3.Models.Trade.Enums;
using Color = Windows.UI.Color;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls.Chart;

public abstract partial class ChartControl<TItem, TDataSourceKernel> : Base.ChartControlBase, IRecipient<OrderAcceptMessage> where TItem : IChartItem where TDataSourceKernel : IDataSourceKernel<TItem>
{
    private readonly SimpleLine _ask = new();
    private readonly SimpleLine _bid = new();
    private readonly SimpleLine _zeroCentury = new();
    private TradeType _tradeType = TradeType.NaN;
    private Line? _openLine;
    private double _open;
    private Line? _slLine;
    private double _sl;
    private double _slLastKnown;
    private Line? _tpLine;
    private double _tp;
    private double _tpLastKnown;
    private const float SquareSize = 10.0f;
    private const float ProximityThresholdStatic = 5.0f;

    protected ChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, TDataSourceKernel dataSource, INotificationsKernel notifications, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, baseColor, quoteColor, logger)
    {
        DataSource = dataSource;
        Notifications = notifications;
        StrongReferenceMessenger.Default.Register(this, new Token(Symbol));
    }

    protected TDataSourceKernel DataSource
    {
        get;
    }

    protected INotificationsKernel Notifications
    {
        get;
    }

    protected static void DrawSquares(CanvasDrawingSession session, NotificationBase notificationBase, Color color)
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

    private static void DrawSquares(CanvasDrawingSession session, Line line, Color color)
    {
        const float halfSquareSize = SquareSize / 2f;
        Vector2 firstPoint, secondPoint;

        // Check if the notificationBase is vertical or horizontal
        if (line.StartPoint.X.Equals(line.EndPoint.X)) // It's a vertical notificationBase
        {
            firstPoint = line.StartPoint.Y < line.EndPoint.Y ? line.StartPoint : line.EndPoint;
            secondPoint = line.StartPoint.Y >= line.EndPoint.Y ? line.StartPoint : line.EndPoint;
            session.FillRectangle(firstPoint.X - halfSquareSize, firstPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
            session.FillRectangle(secondPoint.X - halfSquareSize, secondPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
        }
        else // It's a horizontal notificationBase
        {
            firstPoint = line.StartPoint.X < line.EndPoint.X ? line.StartPoint : line.EndPoint;
            secondPoint = line.StartPoint.X >= line.EndPoint.X ? line.StartPoint : line.EndPoint;
            session.FillRectangle(firstPoint.X - halfSquareSize, firstPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
            session.FillRectangle(secondPoint.X - halfSquareSize, secondPoint.Y - halfSquareSize, SquareSize, SquareSize, color);
        }
    }

    protected override int CalculateKernelShift()
    {
        return (int)Math.Max(0, ((DataSource.Count - Units) / 100d) * (100d - KernelShiftPercent));
    }

    protected override int CalculateKernelShiftPercent()
    {
        return (int)(100d - (KernelShift * 100d) / (DataSource.Count - Units));
    }

    private static bool IsPointOnLine(Windows.Foundation.Point point, SimpleLine sampleLine, double allowableDistance = ProximityThresholdStatic)
    {
        var lineStart = sampleLine.StartPoint;
        var lineEnd = sampleLine.EndPoint;
        var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
        var distance = Math.Abs((lineEnd.X - lineStart.X) * (lineStart.Y - point.Y) - (lineStart.X - point.X) * (lineEnd.Y - lineStart.Y)) / lineLength;
        return distance <= allowableDistance;
    }

    private static bool IsPointOnLine(Windows.Foundation.Point point, NotificationBase notification, double allowableDistance = ProximityThresholdStatic)
    {
        var lineStart = notification.StartPoint;
        var lineEnd = notification.EndPoint;
        var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
        var distance = Math.Abs((lineEnd.X - lineStart.X) * (lineStart.Y - point.Y) - (lineStart.X - point.X) * (lineEnd.Y - lineStart.Y)) / lineLength;
        return distance <= allowableDistance;
    }

    protected void DeselectAllLines()
    {
        if (_slLine != null)
        {
            _slLine.IsSelected = false;
        }

        if (_tpLine != null)
        {
            _tpLine.IsSelected = false;
        }
        
        Notifications.DeSelectAll();
    }

    public void Receive(OrderAcceptMessage message)
    {
        switch (message.Type)
        {
            case AcceptType.Open:
                Debug.Assert(_openLine == null);
                Debug.Assert(message.Symbol == Symbol);
                _openLine = new Line();
                _slLine = new Line();
                _tpLine = new Line();

                _openLine.StartPoint.X = _slLine.StartPoint.X = _tpLine.StartPoint.X = 0;
                _openLine.EndPoint.X = _slLine.EndPoint.X = _tpLine.EndPoint.X = (float)GraphWidth;

                _open = message.Value.Price;
                _sl = _slLastKnown = message.Value.StopLoss;
                _tp = _tpLastKnown = message.Value.TakeProfit;

                _tradeType = message.Value.TradeType;

                EnqueueMessage(MessageType.Information, "Order opened.");
                Invalidate();
                break;
            case AcceptType.Close:
                Debug.Assert(_openLine != null);
                Debug.Assert(message.Symbol == Symbol);
                _openLine = _slLine = _tpLine = null;
                _open = _sl = _tp = default;

                EnqueueMessage(MessageType.Information, "Order closed.");
                Invalidate();
                break;
            case AcceptType.Modify:
                Debug.Assert(_openLine != null);
                Debug.Assert(_slLine != null);
                Debug.Assert(_tpLine != null);
                Debug.Assert(message.Symbol == Symbol);

                _sl = _slLastKnown = message.Value.StopLoss;
                _tp = _tpLastKnown = message.Value.TakeProfit;
                _slLine.IsSelected = _tpLine.IsSelected = false;

                EnqueueMessage(MessageType.Information, "Order modified.");
                Invalidate();
                break;
            case AcceptType.NaN:
        default: throw new ArgumentOutOfRangeException();
        }
    }

    public async Task InitializeAsync()
    {
        var order = await WeakReferenceMessenger.Default.Send(new OrderRequestMessage(Symbol));
        if (order != Order.Null)
        {
            Debug.Assert(_openLine == null);
            _openLine = new Line();
            _slLine = new Line();
            _tpLine = new Line();

            _openLine.StartPoint.X = _slLine.StartPoint.X = _tpLine.StartPoint.X = 0;
            _openLine.EndPoint.X = _slLine.EndPoint.X = _tpLine.EndPoint.X = (float)GraphWidth;

            _open = order.Price;
            _sl = _slLastKnown = order.StopLoss;
            _tp = _tpLastKnown = order.TakeProfit;

            _tradeType = order.TradeType;
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
}