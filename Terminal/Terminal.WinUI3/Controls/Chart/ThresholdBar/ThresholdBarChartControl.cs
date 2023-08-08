/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       ThresholdBarChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using Common.Entities;
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
using System;

namespace Terminal.WinUI3.Controls.Chart.ThresholdBar;

public sealed class ThresholdBarChartControl : ChartControl<Models.Entities.ThresholdBar, ThresholdBars>
{
    private Vector2[] _open = null!;
    private Vector2[] _close = null!;
    private DateTime[] _dateTime = null!;

    private const int MinOcThickness = 3;
    private int _ocThickness = MinOcThickness;
    private const int Space = 1;

    public ThresholdBarChartControl(IConfiguration configuration, ChartSettings chartSettings, double tickValue, ThresholdBars thresholdBars, INotificationsKernel notifications, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, chartSettings, tickValue, thresholdBars, notifications, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(ThresholdBarChartControl);
    }

    protected override int CalculateMaxUnits()
    {
        return (int)Math.Floor((GraphWidth - Space) / (MinOcThickness + Space));
    }

    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        base.GraphCanvas_OnDraw(sender, args);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        CanvasGeometry openCloseUpGeometries;
        CanvasGeometry openCloseDownGeometries;

        DrawData();
        DrawNotifications();
        Execute();
        return;

        void DrawData()
        {
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);
            var end = Math.Min(Units - HorizontalShift, DataSource.Count);

            for (var unit = 0; unit < HorizontalShift; unit++)
            {
                _dateTime[unit] = DateTime.MaxValue;
            }

            for (var unit = 0; unit < end; unit++)
            {
                var open = DataSource[unit + KernelShift].Open;
                var close = DataSource[unit + KernelShift].Close;
                var dateTime = DataSource[unit + KernelShift].Start;

                var yOpen = (ViewPort.High - open) / Digits * VerticalScale;
                var yClose = (ViewPort.High - close) / Digits * VerticalScale;

                _open[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yOpen) : (float)yOpen;
                _close[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yClose) : (float)yClose;
                _dateTime[unit + HorizontalShift] = dateTime;

                if (open < close)
                {
                    openCloseUpCpb.BeginFigure(_close[unit + HorizontalShift]);
                    openCloseUpCpb.AddLine(_open[unit + HorizontalShift]);
                    openCloseUpCpb.EndFigure(CanvasFigureLoop.Open);
                }
                else if (open > close)
                {
                    openCloseDownCpb.BeginFigure(_close[unit + HorizontalShift]);
                    openCloseDownCpb.AddLine(_open[unit + HorizontalShift]);
                    openCloseDownCpb.EndFigure(CanvasFigureLoop.Open);
                }
                else
                {
                    throw new Exception("DrawData: open == close");
                }
            }

            openCloseUpGeometries = CanvasGeometry.CreatePath(openCloseUpCpb);
            openCloseDownGeometries = CanvasGeometry.CreatePath(openCloseDownCpb);
        }

        void DrawNotifications()
        {
            var notifications = Notifications.GetAllNotifications(Symbol, ViewPort);
            foreach (var notification in notifications)
            {
                int index;
                switch (notification.Type)
                {
                    case NotificationType.ThresholdBar:
                        index = Array.IndexOf(_dateTime, ((IDateTimeNotification)notification).DateTime);
                        notification.StartPoint.X = notification.EndPoint.X = _open[index].X;
                        notification.StartPoint.Y = (float)GraphHeight;
                        notification.EndPoint.Y = 0f;
                        DrawLine(notification);
                        args.DrawingSession.DrawText(notification.Description, notification.EndPoint, Colors.Gray, VerticalTextFormat);
                        break;
                    case NotificationType.Candlestick:
                        var givenDateTime = ((IDateTimeNotification)notification).DateTime;
                        var nearestDateTime = DateTime.MaxValue;
                        for (var i = 0; i <= _dateTime.Length - 1; i++)
                        {
                            var dateTime = _dateTime[i];
                            if (_dateTime[i] > givenDateTime)
                            {
                                continue;
                            }

                            nearestDateTime = _dateTime[i];
                            break;
                        }

                        index = Array.IndexOf(_dateTime, nearestDateTime);
                        notification.StartPoint.X = notification.EndPoint.X = _open[index].X;
                        notification.StartPoint.Y = (float)GraphHeight;
                        notification.EndPoint.Y = 0f;
                        DrawLine(notification);
                        args.DrawingSession.DrawText(notification.Description, notification.EndPoint, Colors.Gray, VerticalTextFormat);
                        break;
                    case NotificationType.Price:
                        notification.StartPoint.X = 0f;
                        notification.EndPoint.X = (float)GraphWidth;
                        notification.StartPoint.Y = notification.EndPoint.Y = GetPositionY(((IPriceNotification)notification).Price);
                        DrawLine(notification);
                        args.DrawingSession.DrawText(notification.Description, notification.StartPoint, Colors.Gray, HorizontalTextFormat);
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        void Execute()
        {
            args.DrawingSession.DrawGeometry(openCloseUpGeometries, BaseColor, _ocThickness);
            args.DrawingSession.DrawGeometry(openCloseDownGeometries, QuoteColor, _ocThickness);
        }

        void DrawLine(NotificationBase notification)
        {
            if (!notification.IsSelected)
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
        if (IsVerticalLineRequested)
        {
            DeselectAllLines();
            var index = GetIndex((float)e.GetCurrentPoint(GraphCanvas).Position.X, _open);
            var closestDateTime = _dateTime[index + HorizontalShift];
            var unit = index + KernelShift;
            Debug.Assert(closestDateTime == DataSource[unit].Start);

            var notification = new ThresholdBarChartNotification()
            {
                Symbol = Symbol,
                Type = NotificationType.ThresholdBar,
                DateTime = closestDateTime,
                IsSelected = true,
                Description = DataSource[unit].ToString(),
                StartPoint = new Vector2(),
                EndPoint = new Vector2()
            };

            Notifications.Add(notification);
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

        _open = new Vector2[Units];
        _close = new Vector2[Units];
        _dateTime = new DateTime[Units];

        var chartIndex = 0;
        do
        {
            var x = (float)((Units - chartIndex - 1) * HorizontalScale);
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
    }

    protected override void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        base.GraphCanvas_OnPointerReleased(sender, e);

        //if (VerticalLines.Any(line => line.IsSelected))
        //{
        //    var selectedLine = VerticalLines.FirstOrDefault(line => line.IsSelected);
        //    if (selectedLine != null)
        //    {
        //        var distances = _open.Skip(HorizontalShift).Take(Math.Min(Units - HorizontalShift, DataSource.Count)).
        //            Select((vector, index) => new { Distance = Math.Abs(vector.X - selectedLine.StartPoint.X), Index = index }).ToList();
        //        var minDistanceTuple = distances.Aggregate((a, b) => a.Distance < b.Distance ? a : b);
        //        var index = minDistanceTuple.Index;
        //        var closestVector = _open[index + HorizontalShift];
        //        selectedLine.StartPoint.X = selectedLine.EndPoint.X = closestVector.X;
        //        var unit = index + KernelShift;
        //        selectedLine.Description = DataSource[unit].ToString();
        //        Invalidate();
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("selected vertical line is null");
        //    }
        //}

        //if (HorizontalLines.Any(line => line.IsSelected))
        //{
        //    var selectedLine = HorizontalLines.FirstOrDefault(line => line.IsSelected);
        //    if (selectedLine != null)
        //    {
        //        EnqueueMessage(MessageType.Information, $"y: {selectedLine.StartPoint.Y}");
        //        Invalidate();
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("selected horizontal line is null");
        //    }
        //}
    }

    protected override void MoveSelectedNotification(double deltaX, double deltaY)
    {
        var notification = Notifications.GetSelectedNotification(Symbol);
        switch (notification.Type)
        {
            case NotificationType.Candlestick:
                notification.IsSelected = false;
                break;
            case NotificationType.ThresholdBar:
                var x = notification.StartPoint.X - (float)deltaX;
                var index = GetIndex(x, _open);
                var closestDateTime = _dateTime[index + HorizontalShift];
                var unit = index + KernelShift;
                Debug.Assert(closestDateTime == DataSource[unit].Start);
                ((IDateTimeNotification)notification).DateTime = closestDateTime;
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
            default: throw new ArgumentOutOfRangeException($@" protected override void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)");
        }
    }
}