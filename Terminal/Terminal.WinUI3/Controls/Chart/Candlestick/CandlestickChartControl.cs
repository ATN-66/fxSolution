/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       CandlestickChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Kernels;
using Windows.UI;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.WinUI3.Messenger.Chart;
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Controls.Chart.Candlestick;

public sealed class CandlestickChartControl : ChartControl<Models.Entities.Candlestick, Candlesticks>
{
    private Vector2[] _high = null!;
    private Vector2[] _low = null!;
    private Vector2[] _open = null!;
    private Vector2[] _close = null!;
    private DateTime[] _dateTime = null!;

    private const int MinOcThickness = 3; //todo: make it configurable
    private const int Space = 1; //todo: make it configurable
    private int _ocThickness = MinOcThickness;
    private int _hlThickness = (MinOcThickness - 1) / 2;

    public CandlestickChartControl(IConfiguration configuration, ChartSettings chartSettings, double tickValue, Candlesticks candlesticks, IImpulsesKernel impulses, INotificationsKernel notifications, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, chartSettings, tickValue, candlesticks, impulses, notifications, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(CandlestickChartControl);
    }

    protected override int CalculateMaxUnits()
    {
        return (int) Math.Floor((GraphWidth - Space) / (MinOcThickness + Space));
    }

    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        base.GraphCanvas_OnDraw(sender, args);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        CanvasGeometry highLowUpGeometries;
        CanvasGeometry highLowDownGeometries;
        CanvasGeometry openCloseUpGeometries;
        CanvasGeometry openCloseDownGeometries;
        CanvasGeometry flatGeometries;

        DrawData();
        DrawNotifications();
        Execute();
        return;

        void DrawData()
        {
            try
            {
                using var highLowUpCpb = new CanvasPathBuilder(args.DrawingSession);
                using var highLowDownCpb = new CanvasPathBuilder(args.DrawingSession);
                using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
                using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);
                using var flatCpb = new CanvasPathBuilder(args.DrawingSession);

                var end = Math.Min(Units - HorizontalShift, DataSource.Count);

                for (var unit = 0; unit < HorizontalShift; unit++)
                {
                    _dateTime[unit] = DateTime.MaxValue;
                }

                for (var unit = 0; unit < end; unit++)
                {
                    var high = DataSource[unit + KernelShift].High;
                    var low = DataSource[unit + KernelShift].Low;
                    var open = DataSource[unit + KernelShift].Open;
                    var close = DataSource[unit + KernelShift].Close;
                    var dateTime = DataSource[unit + KernelShift].Start;

                    var yHigh = (ViewPort.High - high) / Digits * VerticalScale;
                    var yLow = (ViewPort.High - low) / Digits * VerticalScale;
                    var yOpen = (ViewPort.High - open) / Digits * VerticalScale;
                    var yClose = (ViewPort.High - close) / Digits * VerticalScale;

                    _high[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yHigh) : (float)yHigh;
                    _low[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yLow) : (float)yLow;
                    _open[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yOpen) : (float)yOpen;
                    _close[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yClose) : (float)yClose;
                    _dateTime[unit + HorizontalShift] = dateTime;

                    if (open < close)
                    {
                        highLowUpCpb.BeginFigure(_low[unit + HorizontalShift]);
                        highLowUpCpb.AddLine(_high[unit + HorizontalShift]);
                        highLowUpCpb.EndFigure(CanvasFigureLoop.Open);

                        openCloseUpCpb.BeginFigure(_close[unit + HorizontalShift]);
                        openCloseUpCpb.AddLine(_open[unit + HorizontalShift]);
                        openCloseUpCpb.EndFigure(CanvasFigureLoop.Open);
                    }
                    else if (open > close)
                    {
                        highLowDownCpb.BeginFigure(_low[unit + HorizontalShift]);
                        highLowDownCpb.AddLine(_high[unit + HorizontalShift]);
                        highLowDownCpb.EndFigure(CanvasFigureLoop.Open);

                        openCloseDownCpb.BeginFigure(_close[unit + HorizontalShift]);
                        openCloseDownCpb.AddLine(_open[unit + HorizontalShift]);
                        openCloseDownCpb.EndFigure(CanvasFigureLoop.Open);
                    }
                    else
                    {
                        flatCpb.BeginFigure(_low[unit + HorizontalShift]);
                        flatCpb.AddLine(_high[unit + HorizontalShift]);
                        flatCpb.EndFigure(CanvasFigureLoop.Open);
                    }
                }

                highLowUpGeometries = CanvasGeometry.CreatePath(highLowUpCpb);
                highLowDownGeometries = CanvasGeometry.CreatePath(highLowDownCpb);
                openCloseUpGeometries = CanvasGeometry.CreatePath(openCloseUpCpb);
                openCloseDownGeometries = CanvasGeometry.CreatePath(openCloseDownCpb);
                flatGeometries = CanvasGeometry.CreatePath(flatCpb);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        void DrawNotifications()
        {
            var notifications = Notifications.GetAllNotifications(Symbol, ViewPort);
            foreach (var notification in notifications)
            {
                switch (notification.Type)
                {
                    case NotificationType.Candlestick:
                        var index = Array.IndexOf(_dateTime, ((IDateTimeNotification)notification).Start);
                        notification.StartPoint.X = notification.EndPoint.X = _open[index].X;
                        notification.StartPoint.Y = (float)GraphHeight;
                        notification.EndPoint.Y = 0f;
                        DrawLine(notification);
                        args.DrawingSession.DrawText(notification.Description, notification.EndPoint, !notification.IsSelected ? Colors.Gray : Colors.White, VerticalTextFormat);
                        break;
                    case NotificationType.Price:
                        notification.StartPoint.X = 0f; 
                        notification.EndPoint.X = (float)GraphWidth;
                        notification.StartPoint.Y = notification.EndPoint.Y = GetPositionY(((IPriceNotification)notification).Price);
                        DrawLine(notification);
                        args.DrawingSession.DrawText(notification.Description, notification.StartPoint, !notification.IsSelected ? Colors.Gray : Colors.White, HorizontalTextFormat);
                        break;
                    default: throw new InvalidOperationException("Unsupported notification type.");
                }
            }
        }

        void Execute()
        {
            args.DrawingSession.DrawGeometry(highLowUpGeometries, BaseColor, _hlThickness);
            args.DrawingSession.DrawGeometry(highLowDownGeometries, QuoteColor, _hlThickness);
            args.DrawingSession.DrawGeometry(openCloseUpGeometries, BaseColor, _ocThickness);
            args.DrawingSession.DrawGeometry(openCloseDownGeometries, QuoteColor, _ocThickness);
            args.DrawingSession.DrawGeometry(flatGeometries, Colors.Gray, _hlThickness);
        }

        void DrawLine(NotificationBase? notification)
        {
            if (!notification!.IsSelected)
            {
                args.DrawingSession.DrawLine(notification.StartPoint, notification.EndPoint, Colors.Gray, 0.5f, NotificationStrokeStyle);
            }
            else
            {
                args.DrawingSession.DrawLine(notification.StartPoint, notification.EndPoint, Colors.Yellow, 1.0f, NotificationStrokeStyleSelected);
                DrawSquares(args.DrawingSession, notification, Colors.Yellow);
            }
        }
    }

    protected override void GraphCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        IsSelected = true;

        if (IsVerticalLineRequested)
        {
            DeselectAllLines();
            var index = GetIndex((float)e.GetCurrentPoint(GraphCanvas).Position.X, _open);
            var closestDateTime = _dateTime[index + HorizontalShift];
            var unit = index + KernelShift;
            Debug.Assert(closestDateTime == DataSource[unit].Start);

            var notification = new CandlestickNotification
            {
                Symbol = Symbol,
                Type = NotificationType.Candlestick,
                Start = closestDateTime,
                IsSelected = true,
                Description = DataSource[unit].ToString(),
                StartPoint = new Vector2(),
                EndPoint = new Vector2()
            };
            
            Notifications.Add(notification);
            EnqueueMessage(MessageType.Trace, notification.ToString());
            Invalidate();
            IsVerticalLineRequested = false;
            return;
        }

        base.GraphCanvas_OnPointerPressed(sender, e);
    }

    protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException("CandlestickChartControl: XAxisCanvas_OnPointerReleased");
    }

    protected override void OnUnitsChanged()
    {
        AdjustThickness();

        HorizontalShift = Math.Clamp(HorizontalShift, 0, Math.Max(0, Units - 1));
        KernelShift = (int)Math.Max(0, ((DataSource.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);

        _high = new Vector2[Units];
        _low = new Vector2[Units];
        _open = new Vector2[Units];
        _close = new Vector2[Units];
        _dateTime = new DateTime[Units];

        var chartIndex = 0;
        do
        {
            var x = (float)((Units - chartIndex - 1) * HorizontalScale);
            _high[chartIndex] = new Vector2 { X = x };
            _low[chartIndex] = new Vector2 { X = x };
            _open[chartIndex] = new Vector2 { X = x };
            _close[chartIndex] = new Vector2 { X = x };
        } while (++chartIndex < Units);

        EnqueueMessage(MessageType.Trace, $"W: {GraphWidth}, maxU: {MaxUnits}, U%: {UnitsPercent}, U: {Units}, HS: {HorizontalShift}, KS: {KernelShift}, KS%: {KernelShiftPercent}");
        Invalidate();
    }

    private void AdjustThickness()
    {
        _ocThickness = (int)Math.Floor((GraphWidth - (Units - 1) * Space) / Units);
        if (_ocThickness < MinOcThickness)
        {
            _ocThickness = MinOcThickness;
        }

        _hlThickness = (int)Math.Sqrt(_ocThickness);

        if (_hlThickness < (MinOcThickness - 1) / 2)
        {
            _hlThickness = (MinOcThickness - 1) / 2;
        }
    }

    protected override void MoveSelectedNotification(double deltaX, double deltaY)
    {
        var notification = Notifications.GetSelectedNotification(Symbol, ViewPort);
        switch (notification!.Type)
        {
            case NotificationType.Candlestick:
                var x = notification.StartPoint.X - (float)deltaX;
                var index = GetIndex(x, _open);
                var closestDateTime = _dateTime[index + HorizontalShift];
                var unit = index + KernelShift;
                Debug.Assert(closestDateTime == DataSource[unit].Start);
                ((IDateTimeNotification)notification).Start = closestDateTime;
                notification.Description = DataSource[unit].ToString();
                break;
            case NotificationType.Price:
                float y;
                if (IsReversed)
                {
                    y = notification.StartPoint.Y + (float)deltaY;
                }
                else
                {
                    y = notification.StartPoint.Y - (float)deltaY;
                }
                var price = GetPrice(y);
                ((IPriceNotification)notification).Price = price;
                notification.Description = price.ToString(PriceTextFormat);
                break;
            default: throw new InvalidOperationException("Unsupported notification type.");
        }
        EnqueueMessage(MessageType.Trace, notification.ToString()!);
    }
    public override void RepeatSelectedNotification()
    {
        if (CommunicationToken is null)
        {
            return;
        }

        var notification = Notifications.GetSelectedNotification(Symbol, ViewPort);
        StrongReferenceMessenger.Default.Send(new ChartMessage(ChartEvent.RepeatSelectedNotification) { ChartType = ChartType, Symbol = Symbol, Notification = notification }, CommunicationToken);
    }
    public override void OnRepeatSelectedNotification(NotificationBase notificationBase)
    {
        DeselectAllLines();
        switch (notificationBase)
        {
            case CandlestickNotification csNotification:
                AddDateTimeNotification(csNotification.Start);
                break;
            case PriceNotification priceNotification:
                if (priceNotification.Symbol != Symbol)
                {
                    return;
                }
                var newPriceNotification = new PriceNotification
                {
                    Symbol = Symbol,
                    Type = NotificationType.Price,
                    Description = priceNotification.Description,
                    IsSelected = false,
                    Price = priceNotification.Price,
                };
                Notifications.Add(newPriceNotification);
                EnqueueMessage(MessageType.Trace, newPriceNotification.ToString());
                break;
            case ThresholdBarNotification tbNotification:
                var dateTimeStart = new DateTime(tbNotification.Start.Year, tbNotification.Start.Month, tbNotification.Start.Day, tbNotification.Start.Hour, tbNotification.Start.Minute, 0);
                AddDateTimeNotification(dateTimeStart);
                var dateTimeEnd = new DateTime(tbNotification.End.Year, tbNotification.End.Month, tbNotification.End.Day, tbNotification.End.Hour, tbNotification.End.Minute, 0);
                if (!dateTimeStart.Equals(dateTimeEnd))
                {
                    AddDateTimeNotification(dateTimeEnd);
                }
                break;
            default: throw new InvalidOperationException("Unsupported notification type.");
        }
        
        Invalidate();
        return;

        void AddDateTimeNotification(DateTime dateTime)
        {
            var candlestick = DataSource.FindItem(dateTime)!;
            var notification = new CandlestickNotification
            {
                Symbol = Symbol,
                Type = NotificationType.Candlestick,
                Description = candlestick.ToString(),
                IsSelected = ViewPort.Start <= dateTime && dateTime <= ViewPort.End,
                Start = candlestick.Start,
            };
            Notifications.Add(notification);
            EnqueueMessage(MessageType.Trace, notification.ToString());
        }
    }
    public override void SaveItems()
    {
        DataSource.SaveItems(Notifications.GetDateTimeRange());
    }
    public override void SaveTransitions()
    {
    }
}