/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                    ChartControl.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Messenger.AccountService;
using Terminal.WinUI3.Models.Notifications;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Controls.Chart;

public abstract partial class ChartControl<TItem, TDataSourceKernel> where TItem : IChartItem where TDataSourceKernel : IDataSourceKernel<TItem>
{
    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        base.GraphCanvas_OnSizeChanged(sender, e);

        _ask.StartPoint.X = _bid.StartPoint.X = 0f;
        _ask.EndPoint.X = _bid.EndPoint.X = (float)e.NewSize.Width;

        if (_openLine != null)
        {
            _openLine.EndPoint.X = (float)e.NewSize.Width;
            _openLine.StartPoint.X = 0;
        }

        if (_slLine != null)
        {
            _slLine.EndPoint.X = (float)e.NewSize.Width;
            _slLine.StartPoint.X = 0;
        }

        if (_tpLine == null)
        {
            return;
        }

        _tpLine.EndPoint.X = (float)e.NewSize.Width;
        _tpLine.StartPoint.X = 0;
    }
    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        var ask = DataSource[KernelShift].Ask;
        var bid = DataSource[KernelShift].Bid;
        ViewPort.High = ask + Pips * Digits / 2d - VerticalShift * Digits;
        ViewPort.Low = ask - Pips * Digits / 2d - VerticalShift * Digits;
        ViewPort.Start = DataSource[Math.Min(Units - HorizontalShift + KernelShift, DataSource.Count) - 1].Start;
        ViewPort.End = DataSource[KernelShift].End;
        

        var yAsk = (ViewPort.High - ask) / Digits * VerticalScale;
        var yBid = (ViewPort.High - bid) / Digits * VerticalScale;

        _ask.StartPoint.Y = _ask.EndPoint.Y = IsReversed ? (float)(GraphHeight - yAsk) : (float)yAsk;
        _bid.StartPoint.Y = _bid.EndPoint.Y = IsReversed ? (float)(GraphHeight - yBid) : (float)yBid;

        args.DrawingSession.DrawLine(_ask.StartPoint, _ask.EndPoint, Colors.Gray, 0.5f);
        args.DrawingSession.DrawLine(_bid.StartPoint, _bid.EndPoint, Colors.Gray, 0.5f);

        if (_openLine != null)
        {
            var yOpen = (ViewPort.High - _open) / Digits * VerticalScale;
            _openLine.StartPoint.Y = _openLine.EndPoint.Y = IsReversed ? (float)(GraphHeight - yOpen) : (float)yOpen;

            using var openStrokeStyle = new CanvasStrokeStyle();
            openStrokeStyle.DashStyle = CanvasDashStyle.DashDotDot;

            switch (_tradeType)
            {
                case TradeType.Buy:
                    args.DrawingSession.DrawLine(_openLine.StartPoint, _openLine.EndPoint, _open < bid ? Colors.Green : Colors.Red, 1.0f, openStrokeStyle);
                    break;
                case TradeType.Sell:
                    args.DrawingSession.DrawLine(_openLine.StartPoint, _openLine.EndPoint, _open > ask ? Colors.Green : Colors.Red, 1.0f, openStrokeStyle);
                    break;
                case TradeType.NaN:
                default: throw new ArgumentOutOfRangeException();
            }
        }

        if (_slLine != null)
        {
            var ySl = (ViewPort.High - _sl) / Digits * VerticalScale;
            _slLine.StartPoint.Y = _slLine.EndPoint.Y = IsReversed ? (float)(GraphHeight - ySl) : (float)ySl;

            using var slCpb = new CanvasPathBuilder(args.DrawingSession);
            slCpb.BeginFigure(_slLine.StartPoint);
            slCpb.AddLine(_slLine.EndPoint);
            slCpb.EndFigure(CanvasFigureLoop.Open);

            using var slGeometries = CanvasGeometry.CreatePath(slCpb);
            using var slStrokeStyle = new CanvasStrokeStyle();
            slStrokeStyle.CustomDashStyle = new float[] { 20, 10 };
            args.DrawingSession.DrawGeometry(slGeometries, Colors.Red, 1.0f, slStrokeStyle);

            if (_slLine.IsSelected)
            {
                DrawSquares(args.DrawingSession, _slLine, Colors.Red);
            }
        }

        if (_tpLine != null)
        {
            var yTp = (ViewPort.High - _tp) / Digits * VerticalScale;
            _tpLine.StartPoint.Y = _tpLine.EndPoint.Y = IsReversed ? (float)(GraphHeight - yTp) : (float)yTp;

            using var tpCpb = new CanvasPathBuilder(args.DrawingSession);
            tpCpb.BeginFigure(_tpLine.StartPoint);
            tpCpb.AddLine(_tpLine.EndPoint);
            tpCpb.EndFigure(CanvasFigureLoop.Open);

            using var tpGeometries = CanvasGeometry.CreatePath(tpCpb);
            using var tpStrokeStyle = new CanvasStrokeStyle();
            tpStrokeStyle.CustomDashStyle = new float[] { 20, 10 };
            args.DrawingSession.DrawGeometry(tpGeometries, Colors.Green, 1.0f, tpStrokeStyle);

            if (_tpLine.IsSelected)
            {
                DrawSquares(args.DrawingSession, _tpLine, Colors.Green);
            }
        }
    }
    protected override void GraphCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (IsHorizontalLineRequested)
        {
            DeselectAllLines();
            var price = GetPrice((float)e.GetCurrentPoint(GraphCanvas).Position.Y);
            var notification = new PriceNotification
            {
                Symbol = Symbol,
                Type = NotificationType.Price,
                Price = price,
                IsSelected = true,
                StartPoint = new Vector2(),
                EndPoint = new Vector2(),
                Description = price.ToString(PriceTextFormat)
            };

            Notifications.Add(notification);
            Invalidate();
            IsHorizontalLineRequested = false;
            return;
        }

        base.GraphCanvas_OnPointerPressed(sender, e);
    }
    protected override void GraphCanvas_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var position = e.GetPosition(GraphCanvas);

        if (_slLine != null && IsPointOnLine(position, _slLine))
        {
            if (!_slLine.IsSelected)
            {
                DeselectAllLines();
                _slLine.IsSelected = true;
            }
            else
            {
                _slLine.IsSelected = false;
            }

            GraphCanvas!.Invalidate();
            return;
        }

        if (_tpLine != null && IsPointOnLine(position, _tpLine))
        {
            if (!_tpLine.IsSelected)
            {
                DeselectAllLines();
                _tpLine.IsSelected = true;
            }
            else
            {
                _tpLine.IsSelected = false;
            }

            GraphCanvas!.Invalidate();
            return;
        }

        foreach (var notification in Notifications.GetAllNotifications(Symbol, ViewPort).Where(notification => IsPointOnLine(position, notification)))
        {
            if (!notification.IsSelected)
            {
                DeselectAllLines();
                notification.IsSelected = true;
            }
            else
            {
                notification.IsSelected = false;
            }

            GraphCanvas!.Invalidate();
            return;
        }
    }
    protected override void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!IsMouseDown)
        {
            return;
        }

        var currentMousePosition = e.GetCurrentPoint(GraphCanvas).Position;
        var currentMouseY = (float)currentMousePosition.Y;
        var currentMouseX = (float)currentMousePosition.X;

        var deltaY = PreviousMouseY - currentMouseY;
        deltaY = IsReversed ? -deltaY : deltaY;
        var deltaX = PreviousMouseX - currentMouseX;

        var pipsChange = deltaY / VerticalScale;
        var unitsChange = deltaX switch
        {
            > 0 => (int)Math.Floor(deltaX / HorizontalScale),
            < 0 => (int)Math.Ceiling(deltaX / HorizontalScale),
            _ => 0
        };

        if (Math.Abs(pipsChange) < 1 && Math.Abs(unitsChange) < 1)
        {
            return;
        }

        if (_slLine is { IsSelected: true })
        {
            if (IsReversed)
            {
                _slLine.StartPoint.Y += (float)deltaY;
                _slLine.EndPoint.Y += (float)deltaY;
                _sl = ViewPort.High - (GraphHeight - _slLine.StartPoint.Y) * Digits / VerticalScale;
            }
            else
            {
                _slLine.StartPoint.Y -= (float)deltaY;
                _slLine.EndPoint.Y -= (float)deltaY;
                _sl = ViewPort.High - _slLine.StartPoint.Y * Digits / VerticalScale;
            }
        }
        else if (_tpLine is { IsSelected: true })
        {
            if (IsReversed)
            {
                _tpLine.StartPoint.Y += (float)deltaY;
                _tpLine.EndPoint.Y += (float)deltaY;
                _tp = ViewPort.High - (GraphHeight - _tpLine.StartPoint.Y) * Digits / VerticalScale;
            }
            else
            {
                _tpLine.StartPoint.Y -= (float)deltaY;
                _tpLine.EndPoint.Y -= (float)deltaY;
                _tp = ViewPort.High - _tpLine.StartPoint.Y * Digits / VerticalScale;
            }
        }
        else 
        {
            var selectedNotification = Notifications.GetSelectedNotification(Symbol, ViewPort);
            if (selectedNotification != null)
            {
                MoveSelectedNotification(deltaX, deltaY);
            }
            else
            {
                VerticalShift += pipsChange;

                switch (HorizontalShift)
                {
                    case 0 when KernelShift == 0:
                        switch (unitsChange)
                        {
                            case > 0:
                                HorizontalShift += unitsChange;
                                HorizontalShift = Math.Clamp(HorizontalShift, 0, Units - 1);
                                break;
                            case < 0:
                                KernelShift -= unitsChange;
                                KernelShift = Math.Clamp(KernelShift, 0, Math.Max(0, DataSource.Count - Units));
                                break;
                        }

                        break;
                    case > 0:
                        Debug.Assert(KernelShift == 0);
                        HorizontalShift += unitsChange;
                        HorizontalShift = Math.Clamp(HorizontalShift, 0, Units - 1);
                        break;
                    default:
                    {
                        if (KernelShift > 0)
                        {
                            Debug.Assert(HorizontalShift == 0);
                            KernelShift -= unitsChange;
                            KernelShift = Math.Clamp(KernelShift, 0, Math.Max(0, DataSource.Count - Units));
                        }
                        else
                        {
                            throw new InvalidOperationException("_horizontalShift <= 0 && _kernelShiftValue <= 0");
                        }

                        break;
                    }
                }
            }
        }

        Invalidate();
        PreviousMouseY = currentMouseY;
        PreviousMouseX = currentMouseX;
    }
    protected async override void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        base.GraphCanvas_OnPointerReleased(sender, e);
        if (!_sl.Equals(_slLastKnown) || !_tp.Equals(_tpLastKnown))
        {
            await StrongReferenceMessenger.Default.Send(new OrderModifyMessage(Symbol, Math.Round(_sl, DecimalPlaces), Math.Round(_tp, DecimalPlaces)));
        }
    }
    
    protected override void CenturyAxisCanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _zeroCentury.StartPoint.X = 0f;
        _zeroCentury.EndPoint.X = (float)e.NewSize.Width;
    }
    protected override void CenturyAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(AxisBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        var offset = IsReversed ? GraphHeight : 0;
        var ask = DataSource[KernelShift].Ask;
        var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        DrawRuler(_openLine == null ? ask : _open);

        void DrawRuler(double price)
        {
            var y = (yZeroPrice - price) / Digits * VerticalScale;
            _zeroCentury.StartPoint.Y = _zeroCentury.EndPoint.Y = IsReversed ? (float)(offset - y) : (float)y;

            var centuriesHeight = GraphHeight / Centuries;
            var distanceToTop = _zeroCentury.StartPoint.Y;
            var distanceToBottom = GraphHeight - _zeroCentury.StartPoint.Y;
            var centuriesAbove = (int)(distanceToTop / centuriesHeight);
            var centuriesBelow = (int)(distanceToBottom / centuriesHeight);

            for (var i = 0; i <= centuriesAbove; i++)
            {
                var yPos = _zeroCentury.StartPoint.Y - (float)(centuriesHeight * i);
                args.DrawingSession.DrawLine(0, yPos, (float)GraphWidth, yPos, AxisForegroundColor, 0.5f);
                var labelValue = -100 * i;
                args.DrawingSession.DrawText(labelValue.ToString("#####0"), 0, yPos, AxisForegroundColor, YAxisCanvasTextFormat);
            }

            for (var i = 1; i <= centuriesBelow; i++)
            {
                var yPos = _zeroCentury.StartPoint.Y + (float)(centuriesHeight * i);
                args.DrawingSession.DrawLine(0, yPos, (float)GraphWidth, yPos, AxisForegroundColor, 0.5f);
                var labelValue = 100 * i;
                args.DrawingSession.DrawText(labelValue.ToString("#####0"), 0, yPos, AxisForegroundColor, YAxisCanvasTextFormat);
            }
        }
    }

    protected override void PipsAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(AxisBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        var offset = IsReversed ? GraphHeight : 0;

        var ask = (float)DataSource[KernelShift].Ask;
        var yZeroPrice = ask + Digits * Pips / 2f - VerticalShift * Digits;
        var y = Math.Abs(offset - (yZeroPrice - ask) / Digits * VerticalScale);
        cpb.BeginFigure(new Vector2(0, (float)y));
        cpb.AddLine(new Vector2((float)PipsAxisWidth, (float)y));
        cpb.EndFigure(CanvasFigureLoop.Open);

        var bid = (float)DataSource[KernelShift].Bid;
        y = Math.Abs(offset - (yZeroPrice - bid) / Digits * VerticalScale);
        cpb.BeginFigure(new Vector2(0f, (float)y));
        cpb.AddLine(new Vector2((float)PipsAxisWidth, (float)y));
        cpb.EndFigure(CanvasFigureLoop.Open);

        var divisor = 1f / (YAxisStepInPips * Digits);
        var firstPriceDivisibleBy10Pips = (float)Math.Floor(yZeroPrice * divisor) / divisor;

        for (var price = firstPriceDivisibleBy10Pips; price >= yZeroPrice - Pips * Digits; price -= Digits * YAxisStepInPips)
        {
            y = Math.Abs(offset - (yZeroPrice - price) / Digits * VerticalScale);
            using var textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(PriceLabelTextFormat), YAxisCanvasTextFormat, (float)PipsAxisWidth, AxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)(y - textLayout.LayoutBounds.Height), AxisForegroundColor);
            textLayout.Dispose();

            cpb.BeginFigure(new Vector2(0f, (float)y));
            cpb.AddLine(new Vector2((float)PipsAxisWidth, (float)y));
            cpb.EndFigure(CanvasFigureLoop.Open);
        }

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), AxisForegroundColor, 0.5f);
    }

    protected override void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(AxisBackgroundColor);
            //using var cpb = new CanvasPathBuilder(args.DrawingSession);
            //args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

            //var startUnit = KernelShift;
            //var endUnit = UnitsPercent - HorizontalShift + KernelShift - 1;

            //var endTime = DataSource[startUnit].Start;
            //var startTime = DataSource[endUnit].Start; //todo
            //var timeSpan = endTime - startTime;
            //var totalSeconds = timeSpan.TotalSeconds;
            //if (totalSeconds <= 0)
            //{
            //    return;
            //}

            //var pixelsPerSecond = GraphWidth / totalSeconds;
            //var minTimeStep = totalSeconds / MaxTicks;
            //var maxTimeStep = totalSeconds / MinTicks;

            //var timeSteps = new List<double> { 1, 5, 10, 30, 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60 };
            //var timeStep = timeSteps.First(t => t >= minTimeStep);
            //if (timeStep > maxTimeStep)
            //{
            //    timeStep = maxTimeStep;
            //}

            //startTime = RoundDateTime(startTime, timeStep);
            //for (double tickTime = 0; tickTime <= totalSeconds; tickTime += timeStep)
            //{
            //    var tickDateTime = startTime.AddSeconds(tickTime);
            //    var x = (float)(tickTime * pixelsPerSecond);

            //    cpb.BeginFigure(new Vector2(x, 0));
            //    cpb.AddLine(new Vector2(x, (float)XAxisHeight));
            //    cpb.EndFigure(CanvasFigureLoop.Open);

            //    var textLayout = new CanvasTextLayout(args.DrawingSession, $"{tickDateTime:t}", axisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            //    args.DrawingSession.DrawTextLayout(textLayout, x + 3, 0, AxisForegroundColor);
            //}

            //args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), AxisForegroundColor, 1);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnDraw");
            throw;
        }
    }

    protected override void TextCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Colors.Transparent);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        var (upCurrency, downCurrency, upColor, downColor) = IsReversed ? (QuoteCurrency, BaseCurrency, QuoteColor, BaseColor) : (BaseCurrency, QuoteCurrency, BaseColor, QuoteColor);
        using var upCurrencyLayout = new CanvasTextLayout(args.DrawingSession, upCurrency.ToString(), CurrencyLabelCanvasTextFormat, 0.0f, CurrencyFontSize);
        using var downCurrencyLayout = new CanvasTextLayout(args.DrawingSession, downCurrency.ToString(), CurrencyLabelCanvasTextFormat, 0.0f, CurrencyFontSize);

        var upCurrencyPosition = new Vector2(0f, 0f);
        var downCurrencyPosition = new Vector2(0f, upCurrencyPosition.Y + (float)upCurrencyLayout.LayoutBounds.Height + 1f);

        args.DrawingSession.DrawTextLayout(upCurrencyLayout, upCurrencyPosition, upColor);
        args.DrawingSession.DrawTextLayout(downCurrencyLayout, downCurrencyPosition, downColor);

        if (IsSelected)
        {
            var borderRect = new Rect(
                upCurrencyPosition.X,
                upCurrencyPosition.Y,
                Math.Max(upCurrencyLayout.LayoutBounds.Width, downCurrencyLayout.LayoutBounds.Width),
                upCurrencyLayout.LayoutBounds.Height + downCurrencyLayout.LayoutBounds.Height + 1f
            );
            args.DrawingSession.DrawRectangle(borderRect, downColor, 1f);
        }

        var askStr = DataSource[KernelShift].Ask.ToString(PriceTextFormat);
        var askHeight = DrawPrice(askStr[..4], askStr[4..6], askStr[6..7], 5, 3, 3);
        var bidStr = DataSource[KernelShift].Bid.ToString(PriceTextFormat);
        var bidHeight = DrawPrice(bidStr[..4], bidStr[4..6], bidStr[6..7], 5, askHeight + 3, 3);
        DrawSpread(DataSource[KernelShift].Ask, DataSource[KernelShift].Bid, 5, bidHeight + 3);

        return;

        float DrawPrice(string firstPart, string secondPart, string thirdPart, float xShift, float yShift, float margin)
        {
            using var priceFirstPartLayout = new CanvasTextLayout(args.DrawingSession, firstPart, AskBidLabelCanvasTextFormat, 0f, 0f);
            using var priceSecondPartLayout = new CanvasTextLayout(args.DrawingSession, secondPart, CurrencyLabelCanvasTextFormat, 0f, 0f);
            using var priceThirdPartLayout = new CanvasTextLayout(args.DrawingSession, thirdPart, AskBidLabelCanvasTextFormat, 0f, 0f);

            var priceThirdPositionX = (float)(GraphWidth - priceThirdPartLayout.DrawBounds.Width - xShift);
            args.DrawingSession.DrawText(thirdPart, priceThirdPositionX, yShift, Colors.Gray, AskBidLabelCanvasTextFormat);

            var priceSecondPositionX = (float)(GraphWidth - priceSecondPartLayout.DrawBounds.Width - priceThirdPartLayout.DrawBounds.Width - xShift - margin);
            args.DrawingSession.DrawText(secondPart, priceSecondPositionX, yShift, Colors.Gray, CurrencyLabelCanvasTextFormat);

            var priceFirstPositionX = (float)(GraphWidth - priceFirstPartLayout.DrawBounds.Width - priceSecondPartLayout.DrawBounds.Width - priceThirdPartLayout.DrawBounds.Width - xShift - margin * 2);
            var priceFirstPositionY = (float)priceSecondPartLayout.DrawBounds.Height - (float)priceFirstPartLayout.DrawBounds.Height + yShift;

            args.DrawingSession.DrawText(firstPart, priceFirstPositionX, priceFirstPositionY, Colors.Gray, AskBidLabelCanvasTextFormat);

            return priceFirstPositionY + (float)priceFirstPartLayout.DrawBounds.Height;
        }

        void DrawSpread(double ask, double bid, float xShift, float yShift)
        {
            var spread = ((ask - bid) / Digits).ToString("###.0");
            using var spreadLayout = new CanvasTextLayout(args.DrawingSession, spread, AskBidLabelCanvasTextFormat, 0f, 0f);
            var spreadPositionX = (float)(GraphWidth - spreadLayout.DrawBounds.Width - xShift);
            args.DrawingSession.DrawText(spread, spreadPositionX, yShift, Colors.Gray, AskBidLabelCanvasTextFormat);
        }
    }

    public override void DeleteSelectedNotification()
    {
        Notifications.DeleteSelected();
        Invalidate();
    }
    public override void DeleteAllNotifications()
    {
        Notifications.DeleteAll();
        Invalidate();
    }
}