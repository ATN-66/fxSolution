/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       ThresholdBarChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Windows.UI;
using Common.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.AI.Data;

namespace Terminal.WinUI3.Controls;

public sealed class ThresholdBarChartControl : ChartControl<ThresholdBar, ThresholdBarKernel>
{
    private Vector2[] _openData = null!;
    private Vector2[] _closeData = null!;

    private const int MinOcThickness = 3;
    private int _ocThickness = MinOcThickness;
    private const int Space = 1;

    public ThresholdBarChartControl(Symbol symbol, bool isReversed, ThresholdBarKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(symbol, isReversed, kernel, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(ThresholdBarChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GraphHeight = (float)e.NewSize.Height;
        Pips = Math.Max(MinPips, MaxPips * PipsPercent / 100);
        GraphWidth = (float)e.NewSize.Width;
        MaxUnits = (int)Math.Floor((GraphWidth - Space) / (MinOcThickness + Space));
        Units = Math.Max(MinUnits, MaxUnits * UnitsPercent / 100);
    }
    
    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(GraphBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
        CanvasGeometry openCloseUpGeometries;
        CanvasGeometry openCloseDownGeometries;

        DrawData();
        Execute();

        void DrawData()
        {
            var offset = IsReversed ? GraphHeight : 0;
            var ask = Kernel[KernelShift].Ask;
            var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;
            
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);

            try
            {
                var end = Math.Min(Units - HorizontalShift, Kernel.Count);

                for (var unit = 0; unit < end; unit++)
                {
                    var open = Kernel[unit + KernelShift].Open;
                    var close = Kernel[unit + KernelShift].Close;

                    var yOpen = (yZeroPrice - open) / Digits * VerticalScale;
                    var yClose = (yZeroPrice - close) / Digits * VerticalScale;

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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
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
        throw new NotImplementedException();
    }

    protected override void OnUnitsChanged()
    {
        AdjustThickness();

        HorizontalShift = Math.Clamp(HorizontalShift, 0, Math.Max(0, Units - 1));
        KernelShift = (int)Math.Max(0, ((Kernel.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);
        VerticalScale = GraphHeight / Pips;

        _openData = new Vector2[Units];
        _closeData = new Vector2[Units];

        for (var unit = 0; unit < Units; unit++)
        {
            var x = (float)((Units - 1 - unit) * HorizontalScale);
            _openData[unit] = new Vector2 { X = x };
            _closeData[unit] = new Vector2 { X = x };
        }

        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
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