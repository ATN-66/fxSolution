/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;
using System.Numerics;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Terminal.WinUI3.AI.Data;
using Microsoft.UI.Xaml;
using System.Drawing.Printing;
using Microsoft.Extensions.Configuration;

namespace Terminal.WinUI3.Controls;

public abstract partial class ChartControl<TItem, TKernel> : ChartControlBase where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected ChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, TKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, baseColor, quoteColor, logger)
    {
        Kernel = kernel;
    }

    protected TKernel Kernel
    {
        get;
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        AskLine.StartPoint.X = 0f;
        AskLine.EndPoint.X = (float)e.NewSize.Width;

        BidLine.StartPoint.X = 0f;
        BidLine.EndPoint.X = (float)e.NewSize.Width;
    }
    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

        var offset = IsReversed ? GraphHeight : 0;
        var ask = Kernel[KernelShift].Ask;
        var bid = Kernel[KernelShift].Bid;
        var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        var yAsk = (yZeroPrice - ask) / Digits * VerticalScale;
        var yBid = (yZeroPrice - bid) / Digits * VerticalScale;

        AskLine.StartPoint.Y = AskLine.EndPoint.Y = IsReversed ? (float)(offset - yAsk) : (float)yAsk;
        BidLine.StartPoint.Y = BidLine.EndPoint.Y = IsReversed ? (float)(offset - yBid) : (float)yBid;

        using var linesCpb = new CanvasPathBuilder(args.DrawingSession);

        linesCpb.BeginFigure(AskLine.StartPoint);
        linesCpb.AddLine(AskLine.EndPoint);
        linesCpb.EndFigure(CanvasFigureLoop.Open);

        linesCpb.BeginFigure(BidLine.StartPoint);
        linesCpb.AddLine(BidLine.EndPoint);
        linesCpb.EndFigure(CanvasFigureLoop.Open);

        using var linesGeometries = CanvasGeometry.CreatePath(linesCpb);
        args.DrawingSession.DrawGeometry(linesGeometries, Colors.Gray, 0.5f);
    }

    protected override void CenturyAxisCanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        base.CenturyAxisCanvasOnSizeChanged(sender, e);

        CenturyZeroLine.StartPoint.X = 0f;
        CenturyZeroLine.EndPoint.X = (float)e.NewSize.Width;
    }

    protected override void CenturyAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(AxisBackgroundColor);

            var offset = IsReversed ? GraphHeight : 0;
            var ask = Kernel[KernelShift].Ask;
            var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;
            var y = (yZeroPrice - ask) / Digits * VerticalScale;
            AskLine.StartPoint.Y = AskLine.EndPoint.Y = IsReversed ? (float)(offset - y) : (float)y;

            var oneTenthOfCenturiesHeight = GraphHeight / Centuries / 10d;
            var distanceToTop = AskLine.StartPoint.Y;
            var distanceToBottom = GraphHeight - AskLine.StartPoint.Y;
            var tenthCenturiesAbove = (int)(distanceToTop / oneTenthOfCenturiesHeight);
            var tenthCenturiesBelow = (int)(distanceToBottom / oneTenthOfCenturiesHeight);

            for (var i = 0; i <= tenthCenturiesAbove; i++)
            {
                var yPos = AskLine.StartPoint.Y - (float)(oneTenthOfCenturiesHeight * i);
                args.DrawingSession.DrawLine(0, yPos, (float)GraphWidth, yPos, Colors.Gray, 0.5f);
                var labelValue = -10 * i;
                args.DrawingSession.DrawText(labelValue.ToString("###0"), 0, yPos, Colors.Gray, YAxisCanvasTextFormat);
            }

            for (var i = 1; i <= tenthCenturiesBelow; i++)
            {
                var yPos = AskLine.StartPoint.Y + (float)(oneTenthOfCenturiesHeight * i);
                args.DrawingSession.DrawLine(0, yPos, (float)GraphWidth, yPos, Colors.Gray, 0.5f);
                var labelValue = 10 * i;
                args.DrawingSession.DrawText(labelValue.ToString("###0"), 0, yPos, Colors.Gray, YAxisCanvasTextFormat);
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "CenturyAxisCanvasOnDraw");
            throw;
        }
    }

    protected override void PipsAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(AxisBackgroundColor);
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

            using var cpb = new CanvasPathBuilder(args.DrawingSession);
            var offset = IsReversed ? GraphHeight : 0;

            var ask = (float)Kernel[KernelShift].Ask;
            var yZeroPrice = ask + Digits * (float)Pips / 2f - VerticalShift * Digits;
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
                using var textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(PriceLabelTextFormat), YAxisCanvasTextFormat, (float)YAxisWidth, (float)AxisFontSize);
                args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)(y - textLayout.LayoutBounds.Height), AxisForegroundColor);
                textLayout.Dispose();

                cpb.BeginFigure(new Vector2(0f, (float)y));
                cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
                cpb.EndFigure(CanvasFigureLoop.Open);
            }

            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), AxisForegroundColor, 1f);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "PipsAxisCanvasOnDraw");
            throw;
        }
    }

    protected override void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(Colors.Transparent);
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
        using var upCurrencyLayout = new CanvasTextLayout(args.DrawingSession, upCurrency, CurrencyLabelCanvasTextFormat, 0.0f, CurrencyFontSize);

        var upCurrencyPosition = new Vector2(0f, 0f);
        var downCurrencyPosition = new Vector2(0f, (float)upCurrencyLayout.DrawBounds.Height + 3f);

        args.DrawingSession.DrawText(upCurrency, upCurrencyPosition, upColor, CurrencyLabelCanvasTextFormat);
        args.DrawingSession.DrawText(downCurrency, downCurrencyPosition, downColor, CurrencyLabelCanvasTextFormat);

        var askStr = Kernel[KernelShift].Ask.ToString(PriceTextFormat);
        var askHeight = DrawPrice(askStr[..4], askStr[4..6], askStr[6..7], 10, 10, 3);
        var bidStr = Kernel[KernelShift].Bid.ToString(PriceTextFormat);
        var bidHeight = DrawPrice(bidStr[..4], bidStr[4..6], bidStr[6..7], 10, askHeight + 3, 3);
        DrawSpread(Kernel[KernelShift].Ask, Kernel[KernelShift].Bid, 10, bidHeight + 3);

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
}