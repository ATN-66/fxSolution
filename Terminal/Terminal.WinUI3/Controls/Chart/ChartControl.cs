/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Messenger.AccountService;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Trade;
using Terminal.WinUI3.Models.Trade.Enums;
using Color = Windows.UI.Color;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls.Chart;

public abstract partial class ChartControl<TItem, TKernel> : Base.ChartControlBase, IRecipient<OrderAcceptMessage> where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected double YZeroPrice;
    private readonly Line _ask = new();
    private readonly Line _bid = new();
    private readonly Line _centuryZeroLine = new();
    private TradeType _tradeType = TradeType.NaN;
    private Line? _priceLine;
    private double _price;
    private Line? _slLine;
    private double _sl;
    private double _slLastKnown;
    private Line? _tpLine;
    private double _tp;
    private double _tpLastKnown;
    private const float SquareSize = 10.0f;
    private const float ProximityThresholdStatic = 5.0f;
    private const float ProximityThresholdDynamic = 30.0f;

    protected ChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, TKernel kernel, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, baseColor, quoteColor, logger)
    {
        Kernel = kernel;
        StrongReferenceMessenger.Default.Register(this, new Token(Symbol));
    }

    protected TKernel Kernel
    {
        get;
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        base.GraphCanvas_OnSizeChanged(sender, e);

        _ask.StartPoint.X = _bid.StartPoint.X = 0f;
        _ask.EndPoint.X = _bid.EndPoint.X = (float)e.NewSize.Width;

        if (_priceLine != null)
        {
            _priceLine.EndPoint.X = (float)e.NewSize.Width;
            _priceLine.StartPoint.X = 0;
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

        var offset = IsReversed ? GraphHeight : 0;
        var ask = Kernel[KernelShift].Ask;
        var bid = Kernel[KernelShift].Bid;
        YZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        var yAsk = (YZeroPrice - ask) / Digits * VerticalScale;
        var yBid = (YZeroPrice - bid) / Digits * VerticalScale;

        _ask.StartPoint.Y = _ask.EndPoint.Y = IsReversed ? (float)(offset - yAsk) : (float)yAsk;
        _bid.StartPoint.Y = _bid.EndPoint.Y = IsReversed ? (float)(offset - yBid) : (float)yBid;

        using var askBidCpb = new CanvasPathBuilder(args.DrawingSession);

        askBidCpb.BeginFigure(_ask.StartPoint);
        askBidCpb.AddLine(_ask.EndPoint);
        askBidCpb.EndFigure(CanvasFigureLoop.Open);

        askBidCpb.BeginFigure(_bid.StartPoint);
        askBidCpb.AddLine(_bid.EndPoint);
        askBidCpb.EndFigure(CanvasFigureLoop.Open);

        using var askBidGeometries = CanvasGeometry.CreatePath(askBidCpb);
        args.DrawingSession.DrawGeometry(askBidGeometries, Colors.Gray, 0.5f);

        if (_priceLine != null)
        {
            var yPrice = (YZeroPrice - _price) / Digits * VerticalScale;
            _priceLine.StartPoint.Y = _priceLine.EndPoint.Y = IsReversed ? (float)(offset - yPrice) : (float)yPrice;
            using var priceCpb = new CanvasPathBuilder(args.DrawingSession);
            priceCpb.BeginFigure(_priceLine.StartPoint);
            priceCpb.AddLine(_priceLine.EndPoint);
            priceCpb.EndFigure(CanvasFigureLoop.Open);
            using var priceGeometries = CanvasGeometry.CreatePath(priceCpb);
            using var priceStrokeStyle = new CanvasStrokeStyle { DashStyle = CanvasDashStyle.DashDotDot };

            switch (_tradeType)
            {
                case TradeType.Buy:
                    args.DrawingSession.DrawGeometry(priceGeometries, _price < bid ? Colors.Green : Colors.Red, 1.0f, priceStrokeStyle);
                    break;
                case TradeType.Sell:
                    args.DrawingSession.DrawGeometry(priceGeometries, _price > ask ? Colors.Green : Colors.Red, 1.0f, priceStrokeStyle);
                    break;
                case TradeType.NaN:
                default: throw new ArgumentOutOfRangeException();
            }
        }

        if (_slLine != null)
        {
            var ySl = (YZeroPrice - _sl) / Digits * VerticalScale;
            _slLine.StartPoint.Y = _slLine.EndPoint.Y = IsReversed ? (float)(offset - ySl) : (float)ySl;

            using var slCpb = new CanvasPathBuilder(args.DrawingSession);
            slCpb.BeginFigure(_slLine.StartPoint);
            slCpb.AddLine(_slLine.EndPoint);
            slCpb.EndFigure(CanvasFigureLoop.Open);

            using var slGeometries = CanvasGeometry.CreatePath(slCpb);
            using var slStrokeStyle = new CanvasStrokeStyle { CustomDashStyle = new float[] { 20, 10 } };
            args.DrawingSession.DrawGeometry(slGeometries, Colors.Red, 1.0f, slStrokeStyle);

            if (_slLine.IsSelected)
            {
                DrawSquare(args.DrawingSession, _slLine.StartPoint, Colors.Red);
                DrawSquare(args.DrawingSession, _slLine.EndPoint, Colors.Red);
            }
        }

        if (_tpLine != null)
        {
            var yTp = (YZeroPrice - _tp) / Digits * VerticalScale;
            _tpLine.StartPoint.Y = _tpLine.EndPoint.Y = IsReversed ? (float)(offset - yTp) : (float)yTp;

            using var tpCpb = new CanvasPathBuilder(args.DrawingSession);
            tpCpb.BeginFigure(_tpLine.StartPoint);
            tpCpb.AddLine(_tpLine.EndPoint);
            tpCpb.EndFigure(CanvasFigureLoop.Open);

            using var tpGeometries = CanvasGeometry.CreatePath(tpCpb);
            using var tpStrokeStyle = new CanvasStrokeStyle { CustomDashStyle = new float[] { 20, 10 } };
            args.DrawingSession.DrawGeometry(tpGeometries, Colors.Green, 1.0f, tpStrokeStyle);

            if (_tpLine.IsSelected)
            {
                DrawSquare(args.DrawingSession, _tpLine.StartPoint, Colors.Green);
                DrawSquare(args.DrawingSession, _tpLine.EndPoint, Colors.Green);
            }
        }
    }

    protected override void GraphCanvas_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var position = e.GetPosition(GraphCanvas);

        if (_slLine != null && IsPointOnLine(position, _slLine))
        {
            _slLine.IsSelected = !_slLine.IsSelected;
            _tpLine!.IsSelected = false;
        }
        else if (_tpLine != null && IsPointOnLine(position, _tpLine))
        {
            _tpLine.IsSelected = !_tpLine.IsSelected;
            _slLine!.IsSelected = false;
        }

        GraphCanvas!.Invalidate();
    }

    protected async override void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        base.GraphCanvas_OnPointerReleased(sender, e);
        if (!_sl.Equals(_slLastKnown) || !_tp.Equals(_tpLastKnown))
        {
            await StrongReferenceMessenger.Default.Send(new OrderModifyMessage(Symbol, Math.Round(_sl, DecimalPlaces), Math.Round(_tp, DecimalPlaces)));
        }
    }

    private static bool IsPointOnLine(Windows.Foundation.Point point, Line line, double allowableDistance = ProximityThresholdStatic)
    {
        var lineStart = line.StartPoint;
        var lineEnd = line.EndPoint;
        var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
        var distance = Math.Abs((lineEnd.X - lineStart.X) * (lineStart.Y - point.Y) - (lineStart.X - point.X) * (lineEnd.Y - lineStart.Y)) / lineLength;
        return distance <= allowableDistance;
    }

    private static void DrawSquare(CanvasDrawingSession session, Vector2 point, Color color)
    {
        const float halfSquareSize = SquareSize / 2f;
        session.FillRectangle(point.X - halfSquareSize, point.Y - halfSquareSize, SquareSize, SquareSize, color);
    }

    protected override void CenturyAxisCanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        base.CenturyAxisCanvasOnSizeChanged(sender, e);

        _centuryZeroLine.StartPoint.X = 0f;
        _centuryZeroLine.EndPoint.X = (float)e.NewSize.Width;
    }

    protected override void CenturyAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(AxisBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        var offset = IsReversed ? GraphHeight : 0;
        var ask = Kernel[KernelShift].Ask;
        var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        DrawRuler(_priceLine == null ? ask : _price);

        void DrawRuler(double price)
        {
            var y = (yZeroPrice - price) / Digits * VerticalScale;
            _centuryZeroLine.StartPoint.Y = _centuryZeroLine.EndPoint.Y = IsReversed ? (float)(offset - y) : (float)y;

            var centuriesHeight = GraphHeight / Centuries;
            var distanceToTop = _centuryZeroLine.StartPoint.Y;
            var distanceToBottom = GraphHeight - _centuryZeroLine.StartPoint.Y;
            var centuriesAbove = (int)(distanceToTop / centuriesHeight);
            var centuriesBelow = (int)(distanceToBottom / centuriesHeight);

            for (var i = 0; i <= centuriesAbove; i++)
            {
                var yPos = _centuryZeroLine.StartPoint.Y - (float)(centuriesHeight * i);
                args.DrawingSession.DrawLine(0, yPos, (float)GraphWidth, yPos, AxisForegroundColor, 0.5f);
                var labelValue = -100 * i;
                args.DrawingSession.DrawText(labelValue.ToString("#####0"), 0, yPos, AxisForegroundColor, YAxisCanvasTextFormat);
            }

            for (var i = 1; i <= centuriesBelow; i++)
            {
                var yPos = _centuryZeroLine.StartPoint.Y + (float)(centuriesHeight * i);
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

        var ask = (float)Kernel[KernelShift].Ask;
        var yZeroPrice = ask + Digits * Pips / 2f - VerticalShift * Digits;
        var y = Math.Abs(offset - (yZeroPrice - ask) / Digits * VerticalScale);
        cpb.BeginFigure(new Vector2(0, (float)y));
        cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
        cpb.EndFigure(CanvasFigureLoop.Open);

        var bid = (float)Kernel[KernelShift].Bid;
        y = Math.Abs(offset - (yZeroPrice - bid) / Digits * VerticalScale);
        cpb.BeginFigure(new Vector2(0f, (float)y));
        cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
        cpb.EndFigure(CanvasFigureLoop.Open);

        var divisor = 1f / (YAxisStepInPips * Digits);
        var firstPriceDivisibleBy10Pips = (float)Math.Floor(yZeroPrice * divisor) / divisor;

        for (var price = firstPriceDivisibleBy10Pips; price >= yZeroPrice - Pips * Digits; price -= Digits * YAxisStepInPips)
        {
            y = Math.Abs(offset - (yZeroPrice - price) / Digits * VerticalScale);
            using var textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(PriceLabelTextFormat), YAxisCanvasTextFormat, (float)YAxisWidth, AxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)(y - textLayout.LayoutBounds.Height), AxisForegroundColor);
            textLayout.Dispose();

            cpb.BeginFigure(new Vector2(0f, (float)y));
            cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
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

            //var endTime = Kernel[startUnit].DateTime;
            //var startTime = Kernel[endUnit].DateTime; //todo
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

        var upCurrencyPosition = new Vector2(0f, 0f);
        var downCurrencyPosition = new Vector2(0f, (float)upCurrencyLayout.DrawBounds.Height + 3f);

        args.DrawingSession.DrawText(upCurrency.ToString(), upCurrencyPosition, upColor, CurrencyLabelCanvasTextFormat);
        args.DrawingSession.DrawText(downCurrency.ToString(), downCurrencyPosition, downColor, CurrencyLabelCanvasTextFormat);

        var askStr = Kernel[KernelShift].Ask.ToString(PriceTextFormat);
        var askHeight = DrawPrice(askStr[..4], askStr[4..6], askStr[6..7], 5, 3, 3);
        var bidStr = Kernel[KernelShift].Bid.ToString(PriceTextFormat);
        var bidHeight = DrawPrice(bidStr[..4], bidStr[4..6], bidStr[6..7], 5, askHeight + 3, 3);
        DrawSpread(Kernel[KernelShift].Ask, Kernel[KernelShift].Bid, 5, bidHeight + 3);

        float DrawPrice(string firstPart, string secondPart, string thirdPart, float xShift, float yShift, float margin)
        {
            using var askFirstPartLayout = new CanvasTextLayout(args.DrawingSession, firstPart, AskBidLabelCanvasTextFormat, 0f, 0f);
            using var askSecondPartLayout = new CanvasTextLayout(args.DrawingSession, secondPart, CurrencyLabelCanvasTextFormat, 0f, 0f);
            using var askThirdPartLayout = new CanvasTextLayout(args.DrawingSession, thirdPart, AskBidLabelCanvasTextFormat, 0f, 0f);

            var askThirdPositionX = (float)(GraphWidth - askThirdPartLayout.DrawBounds.Width - xShift);
            args.DrawingSession.DrawText(thirdPart, askThirdPositionX, yShift, Colors.Gray, AskBidLabelCanvasTextFormat);

            var askSecondPositionX = (float)(GraphWidth - askSecondPartLayout.DrawBounds.Width - askThirdPartLayout.DrawBounds.Width - xShift - margin);
            args.DrawingSession.DrawText(secondPart, askSecondPositionX, yShift, Colors.Gray, CurrencyLabelCanvasTextFormat);

            var askFirstPositionX = (float)(GraphWidth - askFirstPartLayout.DrawBounds.Width - askSecondPartLayout.DrawBounds.Width - askThirdPartLayout.DrawBounds.Width - xShift - margin * 2);
            var askFirstPositionY = (float)askSecondPartLayout.DrawBounds.Height - (float)askFirstPartLayout.DrawBounds.Height + yShift;

            args.DrawingSession.DrawText(firstPart, askFirstPositionX, askFirstPositionY, Colors.Gray, AskBidLabelCanvasTextFormat);

            return askFirstPositionY + (float)askFirstPartLayout.DrawBounds.Height;
        }

        void DrawSpread(double ask, double bid, float xShift, float yShift)
        {
            var spread = ((ask - bid) / Digits).ToString("###.0");
            using var spreadLayout = new CanvasTextLayout(args.DrawingSession, spread, AskBidLabelCanvasTextFormat, 0f, 0f);
            var spreadPositionX = (float)(GraphWidth - spreadLayout.DrawBounds.Width - xShift);
            args.DrawingSession.DrawText(spread, spreadPositionX, yShift, Colors.Gray, AskBidLabelCanvasTextFormat);
        }
    }

    protected override int CalculateKernelShift()
    {
        return (int)Math.Max(0, ((Kernel.Count - Units) / 100d) * (100d - KernelShiftPercent));
    }

    protected override int CalculateKernelShiftPercent()
    {
        return (int)(100d - (KernelShift * 100d) / (Kernel.Count - Units));
    }

    public void Receive(OrderAcceptMessage message)
    {
        switch (message.Type)
        {
            case AcceptType.Open:
                Debug.Assert(_priceLine == null);
                Debug.Assert(message.Symbol == Symbol);
                _priceLine = new Line();
                _slLine = new Line();
                _tpLine = new Line();

                _priceLine.StartPoint.X = _slLine.StartPoint.X = _tpLine.StartPoint.X = 0;
                _priceLine.EndPoint.X = _slLine.EndPoint.X = _tpLine.EndPoint.X = (float)GraphWidth;

                _price = message.Value.Price;
                _sl = _slLastKnown = message.Value.StopLoss;
                _tp = _tpLastKnown = message.Value.TakeProfit;

                _tradeType = message.Value.TradeType;

                EnqueueMessage(MessageType.Information, "Order opened.");
                Invalidate();
                break;
            case AcceptType.Close:
                Debug.Assert(_priceLine != null);
                Debug.Assert(message.Symbol == Symbol);
                _priceLine = _slLine = _tpLine = null;
                _price = _sl = _tp = default;

                EnqueueMessage(MessageType.Information, "Order closed.");
                Invalidate();
                break;
            case AcceptType.Modify:
                Debug.Assert(_priceLine != null);
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
            Debug.Assert(_priceLine == null);
            _priceLine = new Line();
            _slLine = new Line();
            _tpLine = new Line();

            _priceLine.StartPoint.X = _slLine.StartPoint.X = _tpLine.StartPoint.X = 0;
            _priceLine.EndPoint.X = _slLine.EndPoint.X = _tpLine.EndPoint.X = (float)GraphWidth;

            _price = order.Price;
            _sl = _slLastKnown = order.StopLoss;
            _tp = _tpLastKnown = order.TakeProfit;

            _tradeType = order.TradeType;
        }
    }
}