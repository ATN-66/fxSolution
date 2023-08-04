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

namespace Terminal.WinUI3.Controls.Chart.ThresholdBar;

public sealed class ThresholdBarChartControl : ChartControl<Models.Entities.ThresholdBar, ThresholdBars>
{
    private Vector2[] _openData = null!;
    private Vector2[] _closeData = null!;

    private const int MinOcThickness = 3;
    private int _ocThickness = MinOcThickness;
    private const int Space = 1;

    public ThresholdBarChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, ThresholdBars thresholdBars, INotificationsKernel notifications, Color baseColor, Color quoteColor, ILogger<Base.ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, thresholdBars, notifications, baseColor, quoteColor, logger)
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
        Execute();
        return;

        void DrawData()
        {
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);
            var end = Math.Min(Units - HorizontalShift, DataSource.Count);

            for (var unit = 0; unit < end; unit++)
            {
                var open = DataSource[unit + KernelShift].Open;
                var close = DataSource[unit + KernelShift].Close;

                var yOpen = (ViewPort.High - open) / Digits * VerticalScale;
                var yClose = (ViewPort.High - close) / Digits * VerticalScale;

                _openData[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yOpen) : (float)yOpen;
                _closeData[unit + HorizontalShift].Y = IsReversed ? (float)(GraphHeight - yClose) : (float)yClose;

                if (open < close)
                {
                    openCloseUpCpb.BeginFigure(_closeData[unit + HorizontalShift]);
                    openCloseUpCpb.AddLine(_openData[unit + HorizontalShift]);
                    openCloseUpCpb.EndFigure(CanvasFigureLoop.Open);
                }
                else if (open > close)
                {
                    openCloseDownCpb.BeginFigure(_closeData[unit + HorizontalShift]);
                    openCloseDownCpb.AddLine(_openData[unit + HorizontalShift]);
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

        void Execute()
        {
            args.DrawingSession.DrawGeometry(openCloseUpGeometries, BaseColor, _ocThickness);
            args.DrawingSession.DrawGeometry(openCloseDownGeometries, QuoteColor, _ocThickness);
        }
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

        _openData = new Vector2[Units];
        _closeData = new Vector2[Units];

        var chartIndex = 0;
        do
        {
            var x = (float)((Units - chartIndex - 1) * HorizontalScale);
            _openData[chartIndex] = new Vector2 { X = x };
            _closeData[chartIndex] = new Vector2 { X = x };
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
        //        var distances = _openData.Skip(HorizontalShift).Take(Math.Min(Units - HorizontalShift, DataSource.Count)).
        //            Select((vector, index) => new { Distance = Math.Abs(vector.X - selectedLine.StartPoint.X), Index = index }).ToList();
        //        var minDistanceTuple = distances.Aggregate((a, b) => a.Distance < b.Distance ? a : b);
        //        var index = minDistanceTuple.Index;
        //        var closestVector = _openData[index + HorizontalShift];
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

    protected override void MoveSelectedNotification(double deltaX, double deltaY) => throw new NotImplementedException();
}