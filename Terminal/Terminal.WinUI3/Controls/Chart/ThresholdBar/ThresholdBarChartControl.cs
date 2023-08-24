/*+------------------------------------------------------------------+
  |                      Terminal.WinUI3.Controls.Chart.ThresholdBar |
  |                                      ThresholdBarChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Kernels;
using System.Diagnostics;
using Terminal.WinUI3.Models.Notifications;
using Microsoft.UI;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.WinUI3.Messenger.Chart;

namespace Terminal.WinUI3.Controls.Chart.ThresholdBar;

public sealed class ThresholdBarChartControl : ChartControl<Models.Entities.ThresholdBar, ThresholdBars>
{
    readonly Color _leaderImpulseColor = Color.FromArgb(255, 255, 255, 255); // Alpha set to 128 for semi-opacity

    private Vector2[] _thinOpen = null!;
    private Vector2[] _thinClose = null!;
    private Vector2[] _thickOpen = null!;
    private Vector2[] _thickClose = null!;
    private DateTime[] _startDateTime = null!;
    private DateTime[] _endDateTime = null!;

    private const int MinThinOCThickness = 1;
    private const int MinThickOCThickness = 3;
    private int _thinOCThickness = MinThinOCThickness;
    private int _thickOCThickness = MinThickOCThickness;
    private const int Space = 1;

    public ThresholdBarChartControl(IConfiguration configuration, ChartSettings chartSettings, double tickValue, ThresholdBars thresholdBars, IImpulsesKernel impulses, INotificationsKernel notifications, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, chartSettings, tickValue, thresholdBars, impulses, notifications, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(ThresholdBarChartControl);
    }

    protected override int CalculateMaxUnits()
    {
        return (int)Math.Floor((GraphWidth - Space) / (MinThickOCThickness + Space));
    }

    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        base.GraphCanvas_OnDraw(sender, args);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        CanvasGeometry thinOpenCloseUpGeometries;
        CanvasGeometry thinOpenCloseDownGeometries;
        CanvasGeometry thickOpenCloseUpGeometries;
        CanvasGeometry thickOpenCloseDownGeometries;

        DrawData();
        DrawImpulses();
        DrawNotifications();
        return;

        void DrawData()
        {
            try
            {
                using var thinOpenCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
                using var thinOpenCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);
                using var thickOpenCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
                using var thickOpenCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);

                var end = Math.Min(Units - HorizontalShift, DataSource.Count);
                for (var unit = 0; unit < HorizontalShift; unit++)
                {
                    _startDateTime[unit] = DateTime.MaxValue;
                    _endDateTime[unit] = DateTime.MaxValue;
                }

                for (var unit = 0; unit < end; unit++)
                {
                    var thinOpen = DataSource[unit + KernelShift].OpenArray[0];
                    var thinClose = DataSource[unit + KernelShift].CloseArray[0];
                    var thickOpen = DataSource[unit + KernelShift].OpenArray[1];
                    var thickClose = DataSource[unit + KernelShift].CloseArray[1];
                    var startDateTime = DataSource[unit + KernelShift].Start;
                    var endDateTime = DataSource[unit + KernelShift].End;

                    var yThinOpen = (ViewPort.High - thinOpen) / Digits * VerticalScale;
                    var yThinClose = (ViewPort.High - thinClose) / Digits * VerticalScale;
                    var yThickOpen = (ViewPort.High - thickOpen) / Digits * VerticalScale;
                    var yThickClose = (ViewPort.High - thickClose) / Digits * VerticalScale;

                    _thinOpen[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yThinOpen) : (float)yThinOpen;
                    _thinClose[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yThinClose) : (float)yThinClose;
                    _thickOpen[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yThickOpen) : (float)yThickOpen;
                    _thickClose[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yThickClose) : (float)yThickClose;
                    _startDateTime[unit + HorizontalShift] = startDateTime;
                    _endDateTime[unit + HorizontalShift] = endDateTime;

                    if (thinOpen < thickClose)
                    {
                        thinOpenCloseUpCpb.BeginFigure(_thinClose[unit + HorizontalShift]);
                        thinOpenCloseUpCpb.AddLine(_thinOpen[unit + HorizontalShift]);
                        thinOpenCloseUpCpb.EndFigure(CanvasFigureLoop.Open);

                        thickOpenCloseUpCpb.BeginFigure(_thickClose[unit + HorizontalShift]);
                        thickOpenCloseUpCpb.AddLine(_thickOpen[unit + HorizontalShift]);
                        thickOpenCloseUpCpb.EndFigure(CanvasFigureLoop.Open);
                    }
                    else if (thinOpen > thickClose)
                    {
                        thinOpenCloseDownCpb.BeginFigure(_thinClose[unit + HorizontalShift]);
                        thinOpenCloseDownCpb.AddLine(_thinOpen[unit + HorizontalShift]);
                        thinOpenCloseDownCpb.EndFigure(CanvasFigureLoop.Open);

                        thickOpenCloseDownCpb.BeginFigure(_thickClose[unit + HorizontalShift]);
                        thickOpenCloseDownCpb.AddLine(_thickOpen[unit + HorizontalShift]);
                        thickOpenCloseDownCpb.EndFigure(CanvasFigureLoop.Open);
                    }
                    else
                    {
                        throw new Exception("DrawData: open == close");
                    }
                }

                thinOpenCloseUpGeometries = CanvasGeometry.CreatePath(thinOpenCloseUpCpb);
                thinOpenCloseDownGeometries = CanvasGeometry.CreatePath(thinOpenCloseDownCpb);
                thickOpenCloseUpGeometries = CanvasGeometry.CreatePath(thickOpenCloseUpCpb);
                thickOpenCloseDownGeometries = CanvasGeometry.CreatePath(thickOpenCloseDownCpb);

                args.DrawingSession.DrawGeometry(thinOpenCloseUpGeometries, BaseColor, _thinOCThickness);
                args.DrawingSession.DrawGeometry(thinOpenCloseDownGeometries, QuoteColor, _thinOCThickness);
                //args.DrawingSession.DrawGeometry(thinOpenCloseUpGeometries, Color.FromArgb(255, 0, 128, 0), _thinOCThickness);
                //args.DrawingSession.DrawGeometry(thinOpenCloseDownGeometries, Color.FromArgb(255, 128, 0, 0), _thinOCThickness);
                args.DrawingSession.DrawGeometry(thickOpenCloseUpGeometries, BaseColor, _thickOCThickness);
                args.DrawingSession.DrawGeometry(thickOpenCloseDownGeometries, QuoteColor, _thickOCThickness);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        void DrawImpulses()
        {
            try
            {
                var impulses = Impulses.GetAllImpulses(ViewPort);
                foreach (var impulse in impulses)
                {
                    var start = _endDateTime.FirstOrDefault(t => impulse.Start >= t && impulse.Start <= t);//todo
                    int startIndex;
                    if (start == DateTime.MinValue || start == DateTime.MaxValue)
                    {
                        start = _startDateTime.FirstOrDefault(t => impulse.Start >= t && impulse.Start <= t);
                        //Debug.Assert(start != DateTime.MinValue && start != DateTime.MaxValue);
                        startIndex = Array.IndexOf(_startDateTime, start);
                    }
                    else
                    {
                        startIndex = Array.IndexOf(_endDateTime, start);
                    }

                    var end = _endDateTime.FirstOrDefault(t => impulse.End >= t && impulse.End <= t);//todo
                    var endIndex = Array.IndexOf(_endDateTime, end);

                    impulse.StartPoint.X = _thinOpen[startIndex].X;
                    var yStartPoint = (float)((ViewPort.High - impulse.Open) / Digits * VerticalScale);
                    impulse.StartPoint.Y = IsReversed ? (float)(GraphHeight - yStartPoint) : yStartPoint;

                    impulse.EndPoint.X = _thinOpen[endIndex].X;
                    var yEndPoint = (float)((ViewPort.High - impulse.Close) / Digits * VerticalScale);
                    impulse.EndPoint.Y = IsReversed ? (float)(GraphHeight - yEndPoint) : yEndPoint;

                    if (impulse.IsLeader)
                    {
                        args.DrawingSession.DrawLine(impulse.StartPoint, impulse.EndPoint, _leaderImpulseColor, 1f, new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Solid });
                    }
                    else
                    {
                        args.DrawingSession.DrawLine(impulse.StartPoint, impulse.EndPoint, Colors.White, 1f, new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Solid });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        void DrawNotifications()
        {
            var notifications = Notifications.GetAllNotifications(Symbol, ViewPort);
            foreach (var notification in notifications)
            {
                switch (notification.Type)
                {
                    case NotificationType.ThresholdBar:
                        var index = Array.IndexOf(_startDateTime, ((IDateTimeNotification)notification).Start);
                        notification.StartPoint.X = notification.EndPoint.X = _thinOpen[index].X;
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
            var index = GetIndex((float)e.GetCurrentPoint(GraphCanvas).Position.X, _thinOpen);
            var closestDateTime = _startDateTime[index + HorizontalShift];
            var unit = index + KernelShift;
            Debug.Assert(closestDateTime == DataSource[unit].Start);

            var notification = new ThresholdBarNotification()
            {
                Symbol = Symbol,
                Type = NotificationType.ThresholdBar,
                Start = DataSource[unit].Start,
                End = DataSource[unit].End,
                IsSelected = true,
                Description = DataSource[unit].ToString(),
                StartPoint = new Vector2(),
                EndPoint = new Vector2()
            };
            Notifications.Add(notification);
            EnqueueMessage(MessageType.Debug, notification.ToString());
            Invalidate();
            IsVerticalLineRequested = false;
            return;
        }

        base.GraphCanvas_OnPointerPressed(sender, e);
    }

    protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException("ThresholdBarChartControl: XAxisCanvas_OnPointerReleased");
    }

    protected override void OnUnitsChanged()
    {
        AdjustThickness();

        HorizontalShift = Math.Clamp(HorizontalShift, 0, Math.Max(0, Units - 1));
        KernelShift = (int)Math.Max(0, ((DataSource.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);

        _thinOpen = new Vector2[Units];
        _thinClose = new Vector2[Units];
        _thickOpen = new Vector2[Units];
        _thickClose = new Vector2[Units];
        _startDateTime = new DateTime[Units];
        _endDateTime = new DateTime[Units];

        var chartIndex = 0;
        do
        {
            var x = (float)((Units - chartIndex - 1) * HorizontalScale);
            _thinOpen[chartIndex] = new Vector2 { X = x };
            _thinClose[chartIndex] = new Vector2 { X = x };
            _thickOpen[chartIndex] = new Vector2 { X = x };
            _thickClose[chartIndex] = new Vector2 { X = x };
        } while (++chartIndex < Units);

        EnqueueMessage(MessageType.Trace, $"W: {GraphWidth}, maxU: {MaxUnits}, U%: {UnitsPercent}, U: {Units}, HS: {HorizontalShift}, KS: {KernelShift}, KS%: {KernelShiftPercent}");
        Invalidate();
    }

    private void AdjustThickness()
    {
        _thickOCThickness = (int)Math.Floor((GraphWidth - (Units - 1) * Space) / Units);
        _thinOCThickness = _thickOCThickness / 3;
        if (_thinOCThickness < MinThinOCThickness) { _thinOCThickness = MinThinOCThickness; }
        if (_thickOCThickness < MinThickOCThickness) { _thickOCThickness = MinThickOCThickness; }
    }

    protected override void MoveSelectedNotification(double deltaX, double deltaY)
    {
        var notification = Notifications.GetSelectedNotification(Symbol, ViewPort);
        switch (notification!.Type)
        {
            case NotificationType.ThresholdBar:
                var x = notification.StartPoint.X - (float)deltaX;
                var unit = GetIndex(x, _thinOpen) + KernelShift;
                ((IDateTimeRangeNotification)notification).Start = DataSource[unit].Start;
                ((IDateTimeRangeNotification)notification).End = DataSource[unit].End;
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
        EnqueueMessage(MessageType.Debug, notification.ToString()!);
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
        NotificationBase notification;
        switch (notificationBase)
        {
            case CandlestickNotification candlestickNotification:
                var thresholdBar = DataSource.FindItem(candlestickNotification.Start)!;
                notification = new ThresholdBarNotification()
                {
                    Symbol = Symbol,
                    Type = NotificationType.ThresholdBar,
                    Start = thresholdBar.Start,
                    End = thresholdBar.End,
                    IsSelected = ViewPort.Start <= thresholdBar.Start && thresholdBar.End <= ViewPort.End,
                    Description = thresholdBar.ToString(),
                    StartPoint = new Vector2(),
                    EndPoint = new Vector2()
                };
                break;
            case PriceNotification priceNotification:
                if (priceNotification.Symbol != Symbol)
                {
                    return;
                }
                notification = new PriceNotification
                {
                    Symbol = Symbol,
                    Type = NotificationType.Price,
                    Description = priceNotification.Description,
                    IsSelected = ViewPort.High >= priceNotification.Price && priceNotification.Price >= ViewPort.Low,
                    Price = priceNotification.Price,
                };
                break;
            default: throw new InvalidOperationException("Unsupported notification type.");
        }
        Notifications.Add(notification);
        EnqueueMessage(MessageType.Trace, notification.ToString()!);
        Invalidate();
    }
    public override void Save()
    {
        DataSource.Save(Notifications.GetDateTimeRange());
        Impulses.Save(Notifications.GetDateTimeRange());
    }
}