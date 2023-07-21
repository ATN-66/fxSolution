/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       CandlestickChartControl.cs |
  +------------------------------------------------------------------+*/
//#define DEBUGWIN2DCanvasControl

using System.Numerics;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.AI.Data;
using WinRT;
using Microsoft.Graphics.Canvas.Text;

namespace Terminal.WinUI3.Controls;

public sealed class CandlestickChartControl : ChartControl<Candlestick, CandlestickKernel>
{
    private Vector2[] _highData = null!;
    private Vector2[] _lowData = null!;

    public CandlestickChartControl(Symbol symbol, bool isReversed, CandlestickKernel kernel, ILogger<ChartControlBase> logger) : base(symbol, isReversed, kernel, logger)
    {
        DefaultStyleKey = typeof(CandlestickChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            GraphWidth = (float)e.NewSize.Width;
            HeightValue = (float)e.NewSize.Height;
            UnitsPerChart = Math.Clamp(UnitsPerChart, MinUnitsPerChart, MaxUnitsPerChart);

            HorizontalScale = GraphWidth / (UnitsPerChart - 1);
            VerticalScale = HeightValue / PipsPerChart;

            _highData = new Vector2[UnitsPerChart];
            for (var unit = 0; unit < UnitsPerChart; unit++)
            {
                _highData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            }

            _lowData = new Vector2[UnitsPerChart];
            for (var unit = 0; unit < UnitsPerChart; unit++)
            {
                _lowData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnSizeChanged");
            throw;
        }
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
            var ask = (float)Kernel[KernelShiftValue].Close;
            var yZeroPrice = ask + PipsPerChart * Pip / 2f - VerticalShift * Pip;

            var unit = 0;
            while (unit < UnitsPerChart - HorizontalShift)
            {
                _highData[unit + HorizontalShift].Y = (yZeroPrice - (float)Kernel[unit + KernelShiftValue].High) / Pip * VerticalScale;
                _lowData[unit + HorizontalShift].Y = (yZeroPrice - (float)Kernel[unit + KernelShiftValue].Low) / Pip * VerticalScale;

                cpb.BeginFigure(_highData[unit]);
                cpb.AddLine(_lowData[unit]);
                cpb.EndFigure(CanvasFigureLoop.Open);
                unit++;
            }

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
            var startPointToDraw = IsReversed ? line.startPoint with { Y = HeightValue - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = HeightValue - line.endPoint.Y } : line.endPoint;

            cpb.BeginFigure(startPointToDraw);
            cpb.AddLine(endPointToDraw);
            cpb.EndFigure(CanvasFigureLoop.Open);
            var drawLineGeometry = CanvasGeometry.CreatePath(cpb);
            return drawLineGeometry;
        }

        CanvasGeometry GetFillLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            var startPointToDraw = IsReversed ? line.startPoint with { Y = HeightValue - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = HeightValue - line.endPoint.Y } : line.endPoint;

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
        throw new NotImplementedException();
    }

    protected override void OnUnitsPerChartChanged()
    {
    }
}