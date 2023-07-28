﻿/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              TickChartControl.cs |
  +------------------------------------------------------------------+*/

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

namespace Terminal.WinUI3.Controls;

public class TickChartControl : ChartControl<Quotation, QuotationKernel>
{
    private Vector2[] _askData = null!;
    private Vector2[] _bidData = null!;

    public TickChartControl(Symbol symbol, bool isReversed, double tickValue, QuotationKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(symbol, isReversed, tickValue, kernel, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(TickChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GraphHeight = (float)e.NewSize.Height;
        Pips = Math.Max(MinPips, MaxPips * PipsPercent / 100);
        GraphWidth = (float)e.NewSize.Width;
        MaxUnits = (int)GraphWidth;
        Units = Math.Max(MinUnits, MaxUnits * UnitsPercent / 100);
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
            var ask = Kernel[KernelShift].Ask;
            var bid = Kernel[KernelShift].Bid;
            var yZeroPrice = ask + Pips * Digits / 2f - VerticalShift * Digits;

            _askData[HorizontalShift].Y = IsReversed ? (float)(offset - (yZeroPrice - ask) / Digits * VerticalScale) : (float)((yZeroPrice - ask) / Digits * VerticalScale);
            _bidData[HorizontalShift].Y = IsReversed ? (float)(offset - (yZeroPrice - bid) / Digits * VerticalScale) : (float)((yZeroPrice - bid) / Digits * VerticalScale);
            cpb.BeginFigure(_bidData[HorizontalShift]);
            cpb.AddLine(_askData[HorizontalShift]);

            try
            {
                var end = Math.Min(Units - HorizontalShift, Kernel.Count);
                for (var unit = 1; unit < end; unit++)
                {
                    var yAsk = (yZeroPrice - Kernel[unit + KernelShift].Ask) / Digits * VerticalScale;
                    _askData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yAsk) : (float)yAsk;
                    cpb.AddLine(_askData[unit + HorizontalShift]);
                }

                for (var unit = end - 1; unit >= 1; unit--)
                {
                    var yBid = (yZeroPrice - Kernel[unit + KernelShift].Bid) / Digits * VerticalScale;
                    _bidData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yBid) : (float)yBid;
                    cpb.AddLine(_bidData[unit + HorizontalShift]);
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
            var startPointToDraw = IsReversed ? line.startPoint with { Y = (float)GraphHeight - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = (float)GraphHeight - line.endPoint.Y } : line.endPoint;

            cpb.BeginFigure(startPointToDraw);
            cpb.AddLine(endPointToDraw);
            cpb.EndFigure(CanvasFigureLoop.Open);
            var drawLineGeometry = CanvasGeometry.CreatePath(cpb);
            return drawLineGeometry;
        }

        CanvasGeometry GetFillLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            var startPointToDraw = IsReversed ? line.startPoint with { Y = (float)GraphHeight - line.startPoint.Y } : line.startPoint;
            var endPointToDraw = IsReversed ? line.endPoint with { Y = (float)GraphHeight - line.endPoint.Y } : line.endPoint;

            var (arrowHeadLeftPoint, arrowHeadRightPoint) = GetArrowPoints(endPointToDraw, startPointToDraw, (float)ArrowheadLength, (float)ArrowheadWidth);
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
            args.DrawingSession.DrawGeometry(combinedDrawGeometry, GraphForegroundColor, (float)GraphDataStrokeThickness);
            args.DrawingSession.FillGeometry(combinedFillGeometry, GraphForegroundColor);
        }
    }

    protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            throw new NotImplementedException("protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)");

            //UnitsPercent -= (int)Math.Floor(PendingUnitsPerChart);
            //UnitsPercent = Math.Clamp(UnitsPercent, MinUnits, MaxUnitsPerChart);
            //HorizontalScale = GraphWidth / (UnitsPercent - 1);

            //_askData = new Vector2[UnitsPercent];
            //_bidData = new Vector2[UnitsPercent];

            //for (var unit = 0; unit < UnitsPercent; unit++)
            //{
            //    _askData[unit] = new Vector2 { X = (UnitsPercent - 1 - unit) * HorizontalScale };
            //}

            //for (var unit = 0; unit < UnitsPercent; unit++)
            //{
            //    _bidData[unit] = new Vector2 { X = (UnitsPercent - 1 - unit) * HorizontalScale };
            //}
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerReleased");
            throw;
        }

        IsMouseDown = false;
        XAxisCanvas!.ReleasePointerCapture(e.Pointer);

        GraphCanvas!.Invalidate();
        PipsAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
    }

    protected override void OnUnitsChanged()
    {
        HorizontalShift = Math.Clamp(HorizontalShift, 0, Math.Max(0, Units - 1));
        KernelShift = (int)Math.Max(0, ((Kernel.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);
        VerticalScale = GraphHeight / Pips;

        _askData = new Vector2[Units];
        _bidData = new Vector2[Units];

        for (var unit = 0; unit < Units; unit++)
        {
            var x = (float)((Units - 1 - unit) * HorizontalScale);
            _askData[unit] = new Vector2 { X = x };
            _bidData[unit] = new Vector2 { X = x };
        }

        GraphCanvas!.Invalidate();
        PipsAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
    }
}