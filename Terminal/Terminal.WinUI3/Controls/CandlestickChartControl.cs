﻿/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                       CandlestickChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Numerics;
using Windows.UI;
using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Geometry;
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

    private const int MinOcThickness = 3;
    private int _ocThickness = MinOcThickness;
    private int _hlThickness = (MinOcThickness - 1) / 2;
    private const int Space = 1;

    public CandlestickChartControl(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, CandlestickKernel kernel, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger) : base(configuration, symbol, isReversed, tickValue, kernel, baseColor, quoteColor, logger)
    {
        DefaultStyleKey = typeof(CandlestickChartControl);
    }

    protected override void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        base.GraphCanvas_OnSizeChanged(sender, e);

        GraphHeight = e.NewSize.Height;
        Centuries = MinCenturies + (MaxCenturies - MinCenturies) * CenturiesPercent / 100d;

        var pipsPerCentury = Century / TickValue;
        Pips = (int)(pipsPerCentury * Centuries);
        VerticalScale = GraphHeight / Pips;

        GraphWidth = e.NewSize.Width;
        MaxUnits = (int)Math.Floor((GraphWidth - Space) / (MinOcThickness + Space));
        Units = Math.Max(MinUnits, MaxUnits * UnitsPercent / 100);
        HorizontalScale = GraphWidth / (Units - 1);
    }

    protected override void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        base.GraphCanvas_OnDraw(sender, args);

        CanvasGeometry highLowUpGeometries;
        CanvasGeometry highLowDownGeometries;
        CanvasGeometry openCloseUpGeometries;
        CanvasGeometry openCloseDownGeometries;
       
        var ask = Kernel[KernelShift].Ask;
        var yZeroPrice = ask + Pips * Digits / 2d - VerticalShift * Digits;

        DrawData();
        Execute();

        void DrawData()
        {
            var offset = IsReversed ? GraphHeight : 0;
            using var highLowUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var highLowDownCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseUpCpb = new CanvasPathBuilder(args.DrawingSession);
            using var openCloseDownCpb = new CanvasPathBuilder(args.DrawingSession);

            try
            {
                var end = Math.Min(Units - HorizontalShift, Kernel.Count);

                for (var unit = 0; unit < end; unit++)
                {
                    var high = Kernel[unit + KernelShift].High;
                    var low = Kernel[unit + KernelShift].Low;
                    var open = Kernel[unit + KernelShift].Open;
                    var close = Kernel[unit + KernelShift].Close;

                    var yHigh = (yZeroPrice - high) / Digits * VerticalScale;
                    var yLow = (yZeroPrice - low) / Digits * VerticalScale;
                    var yOpen = (yZeroPrice - open) / Digits * VerticalScale;
                    var yClose = (yZeroPrice - close) / Digits * VerticalScale;

                    _highData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yHigh) : (float)yHigh;
                    _lowData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yLow) : (float)yLow;
                    _openData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yOpen) : (float)yOpen;
                    _closeData[unit + HorizontalShift].Y = IsReversed ? (float)(offset - yClose) : (float)yClose;

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
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
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
        throw new NotImplementedException("CandlestickChartControl: XAxisCanvas_OnPointerReleased");
    }

    protected override void OnUnitsChanged()
    {
        AdjustThickness();

        HorizontalShift = Math.Clamp(HorizontalShift, 0, Math.Max(0, Units - 1));
        KernelShift = (int)Math.Max(0, ((Kernel.Count - Units) / 100d) * (100 - KernelShiftPercent));

        HorizontalScale = GraphWidth / (Units - 1);

        _highData = new Vector2[Units];
        _lowData = new Vector2[Units];
        _openData = new Vector2[Units];
        _closeData = new Vector2[Units];

        for (var unit = 0; unit < Units; unit++)
        {
            var x = (float)((Units - 1 - unit) * HorizontalScale);
            _highData[unit] = new Vector2 { X = x };
            _lowData[unit] = new Vector2 { X = x };
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

        _hlThickness = (int)Math.Sqrt(_ocThickness);

        if (_hlThickness < (MinOcThickness - 1) / 2)
        {
            _hlThickness = (MinOcThickness - 1) / 2;
        }
    }
}