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
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Kernel;

namespace Terminal.WinUI3.Controls.Chart.ThresholdBar;

public sealed class ThresholdBarChartControl : ChartControl<Models.Entities.ThresholdBar, ThresholdBarKernel>
{
    private Vector2[] _openData = null!;
    private Vector2[] _closeData = null!;

    private const int MinOcThickness = 3;
    private int _ocThickness = MinOcThickness;
    private const int Space = 1;

    public ThresholdBarChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, ThresholdBarKernel kernel, Color baseColor, Color quoteColor, ILogger<Chart.Base.ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, kernel, baseColor, quoteColor, logger)
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

        var ask = Kernel[KernelShift].Ask;
        YZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        DrawData();
        Execute();

        void DrawData()
        {
            var offset = IsReversed ? GraphHeight : 0;
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);
            var end = Math.Min(Units - HorizontalShift, Kernel.Count);

            for (var unit = 0; unit < end; unit++)
            {
                var open = Kernel[unit + KernelShift].Open;
                var close = Kernel[unit + KernelShift].Close;

                var yOpen = (YZeroPrice - open) / Digits * VerticalScale;
                var yClose = (YZeroPrice - close) / Digits * VerticalScale;

                _openData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yOpen) : (float)yOpen;
                _closeData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yClose) : (float)yClose;

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
        KernelShift = (int)Math.Max(0, ((Kernel.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);

        _openData = new Vector2[Units];
        _closeData = new Vector2[Units];

        for (var unit = 0; unit < Units; unit++)
        {
            var x = (float)((Units - 1 - unit) * HorizontalScale);
            _openData[unit] = new Vector2 { X = x };
            _closeData[unit] = new Vector2 { X = x };
        }

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
}