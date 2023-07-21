/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                                  ChartControl.cs |
  +------------------------------------------------------------------+*/

#define DEBUGWIN2DCanvasControl

using System.Numerics;
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

public abstract class ChartControl<TItem, TKernel> : ChartControlBase where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected ChartControl(Symbol symbol, bool isReversed, TKernel kernel, ILogger<ChartControlBase> logger) : base(symbol, isReversed, logger)
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

            var offset = IsReversed ? HeightValue : 0;

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

    protected override void AdjustMaxUnitsPerChart()
    {
        MaxUnitsPerChart = Math.Min((int)Math.Floor(GraphWidth), Kernel.Count);
        if (MaxUnitsPerChart < MinUnitsPerChart)
        {
            MaxUnitsPerChart = MinUnitsPerChart;
        }
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

#if DEBUGWIN2DCanvasControl
    protected override void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            args.DrawingSession.Clear(GraphBackgroundColor);
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
            var output1 =
                $"width:{GraphWidth:0000}, max units per chart:{MaxUnitsPerChart:0000}, units per chart:{UnitsPerChart:0000}, horizontal shift:{HorizontalShift:0000}, kernel shift:{KernelShiftValue:000000}, kernel.Count:{Kernel.Count:000000}";
            var output2 = $"height:{HeightValue:0000}, pips per chart:{PipsPerChart:0000}, vertical shift:{VerticalShift:###,##}";
            var output3 = $"start Time:{DebugInfoStruct.StartTime}, end time:{DebugInfoStruct.EndTime}, time span:{DebugInfoStruct.TimeSpan:g}, time step:{DebugInfoStruct.TimeStep}, new start time:{DebugInfoStruct.NewStartTime}";

            var textLayout1 = new CanvasTextLayout(args.DrawingSession, output1, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            args.DrawingSession.DrawTextLayout(textLayout1, 0, 0, Colors.White);

            var textLayout2 = new CanvasTextLayout(args.DrawingSession, output2, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            args.DrawingSession.DrawTextLayout(textLayout2, 0, (float)textLayout1.LayoutBounds.Height, Colors.White);

            var textLayout3 = new CanvasTextLayout(args.DrawingSession, output3, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            args.DrawingSession.DrawTextLayout(textLayout3, 0, (float)textLayout1.LayoutBounds.Height * 2, Colors.White);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "DebugCanvas_OnDraw");
            throw;
        }
    }
}
#endif
