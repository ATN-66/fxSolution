/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

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

namespace Terminal.WinUI3.Controls;

public abstract partial class ChartControl<TItem, TKernel> : ChartControlBase where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected ChartControl(Symbol symbol, bool isReversed, TKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(symbol, isReversed, baseColor, quoteColor, logger)
    {
        Kernel = kernel;
    }

    protected TKernel Kernel
    {
        get;
    }

    protected override void YAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(YxAxisBackgroundColor);
            using var cpb = new CanvasPathBuilder(args.DrawingSession);
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

            var offset = IsReversed ? GraphHeight : 0;

            var ask = (float)Kernel[KernelShift].Ask;
            var yZeroPrice = ask + Digits * (float)Pips / 2f - VerticalShift * Digits;
            var y = Math.Abs(offset - (yZeroPrice - ask) / Digits * VerticalScale);
            cpb.BeginFigure(new Vector2(0, (float)y));
            cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
            cpb.EndFigure(CanvasFigureLoop.Open);
            var textLayout = new CanvasTextLayout(args.DrawingSession, ask.ToString(YxAxisLabelFormat), YxAxisTextFormat, (float)YAxisWidth, (float)YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)(y - textLayout.LayoutBounds.Height), YAxisAskBidForegroundColor);

            var bid = (float)Kernel[KernelShift].Bid;
            y = Math.Abs(offset - (yZeroPrice - bid) / Digits * VerticalScale);
            cpb.BeginFigure(new Vector2(0f, (float)y));
            cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
            cpb.EndFigure(CanvasFigureLoop.Open);
            textLayout = new CanvasTextLayout(args.DrawingSession, bid.ToString(YxAxisLabelFormat), YxAxisTextFormat, (float)YAxisWidth, (float)YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)y, YAxisAskBidForegroundColor);

            var divisor = 1f / (YAxisStepInPips * Digits);
            var firstPriceDivisibleBy10Pips = (float)Math.Floor(yZeroPrice * divisor) / divisor;

            for (var price = firstPriceDivisibleBy10Pips; price >= yZeroPrice - Pips * Digits; price -= Digits * YAxisStepInPips)
            {
                y = Math.Abs(offset - (yZeroPrice - price) / Digits * VerticalScale);
                textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(YxAxisLabelFormat), YxAxisTextFormat, (float)YAxisWidth, (float)YAxisFontSize);
                args.DrawingSession.DrawTextLayout(textLayout, 0f, (float)(y - textLayout.LayoutBounds.Height), YxAxisForegroundColor);

                cpb.BeginFigure(new Vector2(0f, (float)y));
                cpb.AddLine(new Vector2((float)YAxisWidth, (float)y));
                cpb.EndFigure(CanvasFigureLoop.Open);
            }

            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), YxAxisForegroundColor, 1);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnDraw");
            throw;
        }
    }

    protected override void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(Colors.BlueViolet);
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

            //    var textLayout = new CanvasTextLayout(args.DrawingSession, $"{tickDateTime:t}", YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            //    args.DrawingSession.DrawTextLayout(textLayout, x + 3, 0, YxAxisForegroundColor);
            //}

            //args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), YxAxisForegroundColor, 1);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnDraw");
            throw;
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