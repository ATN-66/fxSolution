/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              TickChartControl.cs |
  +------------------------------------------------------------------+*/

#define DEBUGWIN2DCanvasControl

using System.Numerics;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.AI.Data;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;

namespace Terminal.WinUI3.Controls;

public class TickChartControl : ChartControl<Quotation, QuotationKernel>
{
    private Vector2[] _askData = null!;
    private Vector2[] _bidData = null!;

    public TickChartControl(Symbol symbol, bool isReversed, QuotationKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(symbol, isReversed, kernel, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(TickChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GraphWidth = (float)e.NewSize.Width;
        GraphHeight = (float)e.NewSize.Height;
        var availableMaxUnitsPerChart = (int)Math.Floor(GraphWidth);
        SetValue(MaxUnitsPerChartProperty, availableMaxUnitsPerChart);
        SetValue(UnitsPerChartProperty, MaxUnitsPerChart);
        OnUnitsPerChartChanged();
    }
    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        var drawGeometries = new List<CanvasGeometry>();
        var fillGeometries = new List<CanvasGeometry>();
        DrawData(drawGeometries, fillGeometries);
        DrawArrowLines(drawGeometries, fillGeometries);
        Execute(drawGeometries, fillGeometries);

        void DrawData(ICollection<CanvasGeometry> dg, ICollection<CanvasGeometry> fg)
        {
            using var drawCpb = new CanvasPathBuilder(args.DrawingSession);
            var drawDataGeometry = GetDrawDataGeometry(drawCpb);
            dg.Add(drawDataGeometry);
            using var fillCpb = new CanvasPathBuilder(args.DrawingSession);
            var fillDataGeometry = GetFillDataGeometry(fillCpb);
            fg.Add(fillDataGeometry);
        }

        CanvasGeometry GetDrawDataGeometry(CanvasPathBuilder cpb)
        {
            var offset = IsReversed ? GraphHeight : 0;

            var ask = (float)Kernel[KernelShiftValue].Ask;
            var yZeroPrice = ask + PipsPerChart * Pip / 2f - VerticalShift * Pip;

            var unit = 0;
            _askData[HorizontalShift].Y = IsReversed ? offset - (yZeroPrice - ask) / Pip * VerticalScale : (yZeroPrice - ask) / Pip * VerticalScale;

            cpb.BeginFigure(_askData[HorizontalShift]);

            try
            {
                while (unit < UnitsPerChart - HorizontalShift)
                {
                    _askData[unit + HorizontalShift].Y =
                        IsReversed ? offset - (yZeroPrice - (float)Kernel[unit + KernelShiftValue].Ask) / Pip * VerticalScale : (yZeroPrice - (float)Kernel[unit + KernelShiftValue].Ask) / Pip * VerticalScale;
                    cpb.AddLine(_askData[unit + HorizontalShift]);
                    unit++;
                }
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnDraw");
                throw;
            }

            unit--;

            try
            {
                while (unit >= 0)
                {
                    _bidData[unit + HorizontalShift].Y = IsReversed ? offset - (yZeroPrice - (float)Kernel[unit + KernelShiftValue].Bid) / Pip * VerticalScale : (yZeroPrice - (float)Kernel[unit + KernelShiftValue].Bid) / Pip * VerticalScale;
                    cpb.AddLine(_bidData[unit + HorizontalShift]);
                    unit--;
                }
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnDraw");
                throw;
            }

            cpb.EndFigure(CanvasFigureLoop.Open);
            var drawDataGeometry = CanvasGeometry.CreatePath(cpb);
            return drawDataGeometry;
        }

        CanvasGeometry GetFillDataGeometry(CanvasPathBuilder cpb)
        {
            var drawDataGeometry = CanvasGeometry.CreatePath(cpb);
            return drawDataGeometry;
        }

        void DrawArrowLines(ICollection<CanvasGeometry> dg, ICollection<CanvasGeometry> fg)
        {
            foreach (var line in ArrowLines)
            {
                using var drawCpb = new CanvasPathBuilder(args.DrawingSession);
                var drawLineGeometry = GetDrawLineGeometry(drawCpb, line);
                dg.Add(drawLineGeometry);
                using var fillCpb = new CanvasPathBuilder(args.DrawingSession);
                var fillLineGeometry = GetFillLineGeometry(fillCpb, line);
                fg.Add(fillLineGeometry);
            }
        }

        CanvasGeometry GetDrawLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            var startPointToDraw = IsReversed ? line.startPoint with { Y = GraphHeight - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = GraphHeight - line.endPoint.Y } : line.endPoint;

            cpb.BeginFigure(startPointToDraw);
            cpb.AddLine(endPointToDraw);
            cpb.EndFigure(CanvasFigureLoop.Open);
            var drawLineGeometry = CanvasGeometry.CreatePath(cpb);
            return drawLineGeometry;
        }

        CanvasGeometry GetFillLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            var startPointToDraw = IsReversed ? line.startPoint with { Y = GraphHeight - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = GraphHeight - line.endPoint.Y } : line.endPoint;

            var (arrowHeadLeftPoint, arrowHeadRightPoint) = GetArrowPoints(endPointToDraw, startPointToDraw, ArrowheadLength, ArrowheadWidth);
            cpb.BeginFigure(endPointToDraw);
            cpb.AddLine(arrowHeadLeftPoint);
            cpb.AddLine(arrowHeadRightPoint);
            cpb.AddLine(endPointToDraw);
            cpb.EndFigure(CanvasFigureLoop.Closed);
            var fillLineGeometry = CanvasGeometry.CreatePath(cpb);
            return fillLineGeometry;
        }

        static (Vector2 arrowHeadLeftPoint, Vector2 arrowHeadRightPoint) GetArrowPoints(Vector2 endPoint, Vector2 startPoint, float arrowheadLength, float arrowheadWidth)
        {
            var direction = Vector2.Normalize(endPoint - startPoint);
            const double angleRadians = Math.PI / 2;

            var arrowHeadLeftDirection = new Vector2(
                (float)(direction.X * Math.Cos(angleRadians) + direction.Y * Math.Sin(angleRadians)),
                (float)(-direction.X * Math.Sin(angleRadians) + direction.Y * Math.Cos(angleRadians))
            );
            var arrowHeadRightDirection = new Vector2(
                (float)(direction.X * Math.Cos(-angleRadians) - direction.Y * Math.Sin(-angleRadians)),
                (float)(direction.X * Math.Sin(-angleRadians) + direction.Y * Math.Cos(-angleRadians))
            );

            var arrowHeadLeftPoint = endPoint - arrowheadLength * direction + arrowheadWidth * arrowHeadLeftDirection;
            var arrowHeadRightPoint = endPoint - arrowheadLength * direction - arrowheadWidth * arrowHeadRightDirection;

            return (arrowHeadLeftPoint, arrowHeadRightPoint);
        }

        void Execute(List<CanvasGeometry> dg, List<CanvasGeometry> fg)
        {
            var combinedDrawGeometry = CanvasGeometry.CreateGroup(args.DrawingSession.Device, dg.ToArray());
            var combinedFillGeometry = CanvasGeometry.CreateGroup(args.DrawingSession.Device, fg.ToArray());
            args.DrawingSession.DrawGeometry(combinedDrawGeometry, GraphForegroundColor, GraphDataStrokeThickness);
            args.DrawingSession.FillGeometry(combinedFillGeometry, GraphForegroundColor);
        }
    }

    protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            UnitsPerChart -= (int)Math.Floor(PendingUnitsPerChart);
            UnitsPerChart = Math.Clamp(UnitsPerChart, MinUnitsPerChart, MaxUnitsPerChart);
            HorizontalScale = GraphWidth / (UnitsPerChart - 1);

            _askData = new Vector2[UnitsPerChart];
            _bidData = new Vector2[UnitsPerChart];

            for (var unit = 0; unit < UnitsPerChart; unit++)
            {
                _askData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            }

            for (var unit = 0; unit < UnitsPerChart; unit++)
            {
                _bidData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerReleased");
            throw;
        }

        IsMouseDown = false;
        XAxisCanvas!.ReleasePointerCapture(e.Pointer);
        PendingUnitsPerChart = 0;

        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
        DebugCanvas!.Invalidate();
#endif
    }

    protected override void OnUnitsPerChartChanged()
    {
        if (UnitsPerChart + KernelShift > Kernel.Count)
        {
            KernelShift = Kernel.Count - UnitsPerChart;
            if (KernelShift < 0)
            {
                throw new Exception("OnUnitsPerChartChanged: KernelShift < 0");
            }
        }

        HorizontalScale = GraphWidth / (UnitsPerChart - 1);
        VerticalScale = GraphHeight / PipsPerChart;

        _askData = new Vector2[UnitsPerChart];
        _bidData = new Vector2[UnitsPerChart];

        for (var unit = 0; unit < UnitsPerChart; unit++)
        {
            _askData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            _bidData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
        }

        var range = Kernel.Count - UnitsPerChart;
        if (range == 0)
        {
            KernelShiftPercent = 100;
        }

        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
        DebugCanvas!.Invalidate();
#endif
    }

#if DEBUGWIN2DCanvasControl
    protected override void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        var output1 = $"width:{GraphWidth:0000}, max units:{MaxUnitsPerChart:0000}, units:{UnitsPerChart:0000}, horizontal shift:{HorizontalShift:0000}, kernel shift:{KernelShiftValue:000000}, kernel.Count:{Kernel.Count:000000}";
        //var output2 = $"_ocThickness:{_ocThickness:###,##}, _hlThickness:{_hlThickness:###,##}";

        //var output2 = $"height:{GraphHeight:0000}, pips per chart:{PipsPerChart:0000}, vertical shift:{VerticalShift:###,##}";
        //var output3 = $"start Time:{DebugInfoStruct.StartTime}, end time:{DebugInfoStruct.EndTime}, time span:{DebugInfoStruct.TimeSpan:g}, time step:{DebugInfoStruct.TimeStep}, new start time:{DebugInfoStruct.NewStartTime}";

        var textLayout1 = new CanvasTextLayout(args.DrawingSession, output1, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        args.DrawingSession.DrawTextLayout(textLayout1, 0, 0, Colors.White);

        //var textLayout2 = new CanvasTextLayout(args.DrawingSession, output2, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        //args.DrawingSession.DrawTextLayout(textLayout2, 0, (float)textLayout1.LayoutBounds.Height, Colors.White);

        //var textLayout3 = new CanvasTextLayout(args.DrawingSession, output3, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        //args.DrawingSession.DrawTextLayout(textLayout3, 0, (float)textLayout1.LayoutBounds.Height * 2, Colors.White);
    }
#endif
}