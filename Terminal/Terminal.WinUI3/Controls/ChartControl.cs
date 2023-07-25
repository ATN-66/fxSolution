﻿/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

#define DEBUGWIN2DCanvasControl

using System.Numerics;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Terminal.WinUI3.AI.Data;

namespace Terminal.WinUI3.Controls;

public abstract class ChartControl<TItem, TKernel> : ChartControlBase where TItem : IChartItem where TKernel : IKernel<TItem>
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

            var ask = (float)Kernel[KernelShiftValue].Ask;
            var yZeroPrice = ask + Pip * PipsPerChart / 2f - VerticalShift * Pip;
            var y = Math.Abs(offset - (yZeroPrice - ask) / Pip * VerticalScale);
            cpb.BeginFigure(new Vector2(0, y));
            cpb.AddLine(new Vector2(YAxisWidth, y));
            cpb.EndFigure(CanvasFigureLoop.Open);
            var textLayout = new CanvasTextLayout(args.DrawingSession, ask.ToString(YxAxisLabelFormat), YxAxisTextFormat, YAxisWidth, YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0, y - (float)textLayout.LayoutBounds.Height, YAxisAskBidForegroundColor);

            var bid = (float)Kernel[KernelShiftValue].Bid;
            y = Math.Abs(offset - (yZeroPrice - bid) / Pip * VerticalScale);
            cpb.BeginFigure(new Vector2(0, y));
            cpb.AddLine(new Vector2(YAxisWidth, y));
            cpb.EndFigure(CanvasFigureLoop.Open);
            textLayout = new CanvasTextLayout(args.DrawingSession, bid.ToString(YxAxisLabelFormat), YxAxisTextFormat, YAxisWidth, YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0, y, YAxisAskBidForegroundColor);

            var divisor = 1f / (YAxisStepInPips * Pip);
            var firstPriceDivisibleBy10Pips = (float)Math.Floor(yZeroPrice * divisor) / divisor;

            for (var price = firstPriceDivisibleBy10Pips; price >= yZeroPrice - PipsPerChart * Pip; price -= Pip * YAxisStepInPips)
            {
                y = Math.Abs(offset - (yZeroPrice - price) / Pip * VerticalScale);
                textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(YxAxisLabelFormat), YxAxisTextFormat, YAxisWidth, YAxisFontSize);
                args.DrawingSession.DrawTextLayout(textLayout, 0, y - (float)textLayout.LayoutBounds.Height, YxAxisForegroundColor);

                cpb.BeginFigure(new Vector2(0, y));
                cpb.AddLine(new Vector2(YAxisWidth, y));
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
            args.DrawingSession.Clear(YxAxisBackgroundColor);
            using var cpb = new CanvasPathBuilder(args.DrawingSession);
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

            var startUnit = KernelShiftValue;
            var endUnit = UnitsPerChart - HorizontalShift + KernelShiftValue - 1;

            var endTime = Kernel[startUnit].DateTime;
            var startTime = Kernel[endUnit].DateTime; //todo
            var timeSpan = endTime - startTime;
            var totalSeconds = timeSpan.TotalSeconds;
            if (totalSeconds <= 0)
            {
                return;
            }

            var pixelsPerSecond = GraphWidth / totalSeconds;
#if DEBUGWIN2DCanvasControl
            DebugInfoStruct.StartTime = startTime;
            DebugInfoStruct.EndTime = endTime;
            DebugInfoStruct.TimeSpan = timeSpan;
#endif
            var minTimeStep = totalSeconds / MaxTicks;
            var maxTimeStep = totalSeconds / MinTicks;

            var timeSteps = new List<double> { 1, 5, 10, 30, 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60 };
            var timeStep = timeSteps.First(t => t >= minTimeStep);
            if (timeStep > maxTimeStep)
            {
                timeStep = maxTimeStep;
            }

            startTime = RoundDateTime(startTime, timeStep);
#if DEBUGWIN2DCanvasControl
            DebugInfoStruct.TimeStep = timeStep;
            DebugInfoStruct.NewStartTime = startTime;
#endif
            for (double tickTime = 0; tickTime <= totalSeconds; tickTime += timeStep)
            {
                var tickDateTime = startTime.AddSeconds(tickTime);
                var x = (float)(tickTime * pixelsPerSecond);

                cpb.BeginFigure(new Vector2(x, 0));
                cpb.AddLine(new Vector2(x, XAxisHeight));
                cpb.EndFigure(CanvasFigureLoop.Open);

                var textLayout = new CanvasTextLayout(args.DrawingSession, $"{tickDateTime:t}", YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
                args.DrawingSession.DrawTextLayout(textLayout, x + 3, 0, YxAxisForegroundColor);
            }

            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), YxAxisForegroundColor, 1);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnDraw");
            throw;
        }
    }

    protected override void OnKernelShiftPercentChanged(double value)
    {
        try
        {
            if (HorizontalShift > 0)
            {
                HorizontalShift = 0;
            }

            var range = Kernel.Count - UnitsPerChart;
            KernelShiftValue = (int)(range - value / 100d * range);

            if (range == 0)
            {
                if (!value.Equals(100d))
                {
                    throw new InvalidOperationException("!value.Equals(100d)");
                }

                return;
            }

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
            DebugCanvas!.Invalidate();
#endif
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "OnKernelShiftPercentChanged");
            throw;
        }
    }

    protected override double AdjustRangeOnKernelShiftPercent(double value)
    {
        var range = Kernel.Count - UnitsPerChart;
        if (range == 0)
        {
            value = 100;
        }

        return value;
    }

    protected override void AdjustKernelShift()
    {
        KernelShiftValue = Math.Clamp(KernelShiftValue, 0, Kernel.Count - UnitsPerChart);
        EnableDrawing = false;
        var range = Kernel.Count - UnitsPerChart;
        if (range != 0)
        {
            KernelShiftPercent = (range - KernelShiftValue) / (double)range * 100;
        }
        else
        {
            KernelShiftPercent = 100;
        }

        EnableDrawing = true;
    }
}