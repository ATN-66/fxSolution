/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       CandlestickChartControl.cs |
  +------------------------------------------------------------------+*/
#define DEBUGWIN2DCanvasControl

using System.Numerics;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.AI.Data;

namespace Terminal.WinUI3.Controls;

public sealed class CandlestickChartControl : ChartControl<Candlestick, CandlestickKernel>
{
    private Vector2[] _highData = null!;
    private Vector2[] _lowData = null!;
    private Vector2[] _openData = null!;
    private Vector2[] _closeData = null!;

    private const float MinOcThickness = 2f;
    private float _ocThickness = MinOcThickness;
    private float _hlThickness = MinOcThickness / 2;
    private const float Space = 1f;

    public CandlestickChartControl(Symbol symbol, bool isReversed, CandlestickKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(symbol, isReversed, kernel, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(CandlestickChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GraphWidth = (float)e.NewSize.Width;
        GraphHeight = (float)e.NewSize.Height;
        UnitsPerChart = Math.Clamp(UnitsPerChart, MinUnitsPerChart, MaxUnitsPerChart);

        AdjustThickness();

        HorizontalScale = GraphWidth / (UnitsPerChart - 1);
        VerticalScale = GraphHeight / PipsPerChart;

        _highData = new Vector2[UnitsPerChart];
        _lowData = new Vector2[UnitsPerChart];
        _openData = new Vector2[UnitsPerChart];
        _closeData = new Vector2[UnitsPerChart];

        for (var unit = 0; unit < UnitsPerChart; unit++)
        {
            _highData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            _lowData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            _openData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
            _closeData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
        }
    }
    
    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        CanvasGeometry highLowUpGeometries;
        CanvasGeometry highLowDownGeometries;
        CanvasGeometry openCloseUpGeometries;
        CanvasGeometry openCloseDownGeometries;

        DrawData();
        Execute();

        void DrawData()
        {
            var offset = IsReversed ? GraphHeight : 0;
            var ask = (float)Kernel[KernelShiftValue].Ask;
            var yZeroPrice = ask + PipsPerChart * Pip / 2f - VerticalShift * Pip;
            var unit = 0;

            using var highLowUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var highLowDownCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);

            while (unit < UnitsPerChart - HorizontalShift)
            {
                var high = (float)Kernel[unit + KernelShiftValue].High;
                var low = (float)Kernel[unit + KernelShiftValue].Low;
                var open = (float)Kernel[unit + KernelShiftValue].Open;
                var close = (float)Kernel[unit + KernelShiftValue].Close;

                var yHigh = (yZeroPrice - high) / Pip * VerticalScale;
                var yLow = (yZeroPrice - low) / Pip * VerticalScale;
                var yOpen = (yZeroPrice - open) / Pip * VerticalScale;
                var yClose = (yZeroPrice - close) / Pip * VerticalScale;

                _highData[unit + HorizontalShift].Y = IsReversed ? offset - yHigh : yHigh;
                _lowData[unit + HorizontalShift].Y = IsReversed ? offset - yLow : yLow;
                _openData[unit + HorizontalShift].Y = IsReversed ? offset - yOpen : yOpen;
                _closeData[unit + HorizontalShift].Y = IsReversed ? offset - yClose : yClose;

                if (open < close)
                {
                    highLowUpCpb.BeginFigure(_lowData[unit + HorizontalShift]);
                    highLowUpCpb.AddLine(_highData[unit + HorizontalShift]);
                    highLowUpCpb.EndFigure(CanvasFigureLoop.Open);

                    openCloseUpCpb.BeginFigure(_closeData[unit + HorizontalShift]);
                    openCloseUpCpb.AddLine(_openData[unit + HorizontalShift]);
                    openCloseUpCpb.EndFigure(CanvasFigureLoop.Open);
                }
                else if (open > close)
                {
                    highLowDownCpb.BeginFigure(_lowData[unit + HorizontalShift]);
                    highLowDownCpb.AddLine(_highData[unit + HorizontalShift]);
                    highLowDownCpb.EndFigure(CanvasFigureLoop.Open);

                    openCloseDownCpb.BeginFigure(_closeData[unit + HorizontalShift]);
                    openCloseDownCpb.AddLine(_openData[unit + HorizontalShift]);
                    openCloseDownCpb.EndFigure(CanvasFigureLoop.Open);
                }
                else
                {
                    //todo
                }
                
                unit++;
            }

            highLowUpGeometries = CanvasGeometry.CreatePath(highLowUpCpb);
            highLowDownGeometries = CanvasGeometry.CreatePath(highLowDownCpb);
            openCloseUpGeometries = CanvasGeometry.CreatePath(openCloseUpCpb);
            openCloseDownGeometries = CanvasGeometry.CreatePath(openCloseDownCpb);
        }

        void Execute()
        {
            args.DrawingSession.DrawGeometry(highLowUpGeometries, BaseColor, _hlThickness);
            args.DrawingSession.DrawGeometry(highLowDownGeometries, QuoteColor, _hlThickness);
            args.DrawingSession.DrawGeometry(openCloseUpGeometries, BaseColor, _ocThickness);
            args.DrawingSession.DrawGeometry(openCloseDownGeometries, QuoteColor, _ocThickness);
        }
    }

    protected override void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    protected override void OnUnitsPerChartChanged()
    {
        try
        {
            if (UnitsPerChart + KernelShift > Kernel.Count)
            {
                KernelShift = Kernel.Count - UnitsPerChart;
            }

            AdjustThickness();

            HorizontalScale = GraphWidth / (UnitsPerChart - 1);

            _highData = new Vector2[UnitsPerChart];
            _lowData = new Vector2[UnitsPerChart];
            _openData= new Vector2[UnitsPerChart];
            _closeData = new Vector2[UnitsPerChart];

            for (var unit = 0; unit < UnitsPerChart; unit++)
            {
                _highData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
                _lowData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
                _openData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
                _closeData[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit) * HorizontalScale };
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
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "OnUnitsPerChartChanged");
            throw;
        }
    }

    private void AdjustThickness()
    {
        _ocThickness = (GraphWidth / UnitsPerChart) - 2f;

        if (_ocThickness < MinOcThickness)
        {
            _ocThickness = MinOcThickness;
        }

        _hlThickness = MinOcThickness / 2f;
    }

    protected override void AdjustMaxUnitsPerChart()
    {
        MaxUnitsPerChart = (int)Math.Floor(GraphWidth / (MinOcThickness + Space));
        MaxUnitsPerChart = Math.Min(MaxUnitsPerChart, Kernel.Count);
        MaxUnitsPerChart = Math.Max(MaxUnitsPerChart, MinUnitsPerChart);
    }   
}