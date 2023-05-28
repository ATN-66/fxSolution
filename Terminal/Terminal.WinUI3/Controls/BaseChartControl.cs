﻿/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              BaseChartControl.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.AI.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using System.Diagnostics;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls;

public class BaseChartControl : Control
{
    private CanvasControl? _graphCanvas;
    private CanvasControl? _yAxisCanvas;
    private CanvasControl? _xAxisCanvas;
    private readonly Kernel _kernel;
    private Vector2[] _data = null!;

    private readonly float _pip;
    private const float YAxisStepInPips = 10f; //todo: symbol dependent ???
    private float _width;
    private float _yAxisWidth;
    private float _height;
    private float _xAxisHeight;
    private int _unitsPerChart = 1000; //axis X //todo: settings
    private float _pendingUnitsPerChart;
    private const int MaxUnitsPerChart = 5000; //todo: settings
    private const int MinUnitsPerChart = 10; //todo: settings
    private float _pipsPerChart = 30; //axis Y //todo: settings
    private float _horizontalScale;
    private float _verticalScale;

    private float _verticalShiftInPips; // positive moves graph up, negative moves graph down
    private int _horizontalGraphShiftInUnits; //It has to be >= 0 and < UnitsPerChart
    private int _horizontalKernelShiftInUnits;

    private const float YAxisFontSize = 12;
    private const string YAxisFontFamily = "Lucida Console";
    private readonly CanvasTextFormat _yAxisTextFormat = new() { FontSize = YAxisFontSize, FontFamily = YAxisFontFamily };
    private readonly string _yAxisTextExample;
    private float _yAxisTextWidth;
    private const float YAxisAdjustment = 3;

    private const float GraphDataStrokeThickness = 1;
    private readonly Color _graphBackgroundColor = Colors.Black;
    private readonly Color _graphForegroundColor = Colors.White;
    private readonly Color _yAxisForegroundColor = Colors.Gray;
    private readonly Color _yAxisAskBidForegroundColor = Colors.White;
    private const string HexCode = "#202020"; // Raisin Black color
    private readonly string _yxAxisLabelFormat;
    private readonly  Color _yxAxisBackgroundColor = Color.FromArgb(
        255,
        Convert.ToByte(HexCode.Substring(1, 2), 16),
        Convert.ToByte(HexCode.Substring(3, 2), 16),
        Convert.ToByte(HexCode.Substring(5, 2), 16)
    );

    private bool _isMouseDown;
    private float _previousMouseY;
    private float _previousMouseX;

    public BaseChartControl(Kernel kernel, Common.Entities.Symbol symbol, bool isOpposite)
    {
        if (isOpposite) throw new NotImplementedException();

        DefaultStyleKey = typeof(BaseChartControl);
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        switch (symbol)
        {
            case Symbol.EURGBP:
            case Symbol.EURUSD:
            case Symbol.GBPUSD:
                _pip = 0.0001f;
                _yAxisTextExample = "0.12345";
                _yxAxisLabelFormat = "f5";
                break;
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                _pip = 0.01f;
                _yAxisTextExample = "012.345";
                _yxAxisLabelFormat = "f3";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _graphCanvas = GetTemplateChild("graphCanvas") as CanvasControl;
        _yAxisCanvas = GetTemplateChild("yAxisCanvas") as CanvasControl;
        _xAxisCanvas = GetTemplateChild("xAxisCanvas") as CanvasControl;

        if (_graphCanvas == null || _yAxisCanvas == null || _xAxisCanvas == null)
        {
            throw new InvalidOperationException("Canvas controls not found in the template.");
        }

        _graphCanvas.SizeChanged += OnGraphCanvasSizeChanged;
        _graphCanvas.Draw += GraphCanvas_OnDraw;
        _graphCanvas.PointerPressed += OnGraphCanvasPointerPressed;
        _graphCanvas.PointerMoved += OnGraphCanvasPointerMoved;
        _graphCanvas.PointerReleased += OnGraphCanvasPointerReleased;

        _yAxisCanvas.SizeChanged += OnYAxisCanvasSizeChanged;
        _yAxisCanvas.Draw += YAxisCanvasOnDraw;
        _yAxisCanvas.PointerEntered += OnYAxisCanvasPointerEntered;
        _yAxisCanvas.PointerExited += OnYAxisCanvasPointerExited;
        _yAxisCanvas.PointerPressed += OnYAxisCanvasPointerPressed;
        _yAxisCanvas.PointerMoved += OnYAxisCanvasPointerMoved;
        _yAxisCanvas.PointerReleased += OnYAxisCanvasPointerReleased;

        _xAxisCanvas.SizeChanged += OnXAxisCanvasSizeChanged;
        _xAxisCanvas.Draw += XAxisCanvasOnDraw;
        _xAxisCanvas.PointerEntered += OnXAxisCanvasPointerEntered;
        _xAxisCanvas.PointerExited += OnXAxisCanvasPointerExited;
        _xAxisCanvas.PointerPressed += OnXAxisCanvasPointerPressed;
        _xAxisCanvas.PointerMoved += OnXAxisCanvasPointerMoved;
        _xAxisCanvas.PointerReleased += OnXAxisCanvasPointerReleased;
    }

    private void OnGraphCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _width = (float)e.NewSize.Width;
        _height = (float)e.NewSize.Height;
        _horizontalScale = _width / (_unitsPerChart - 1);
        _verticalScale = _height / (_pipsPerChart - 1);

        _data = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _data[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }
    }

    private void OnYAxisCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var yAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        _yAxisTextWidth = CalculateAxisCanvasWidth();
        _yAxisWidth = _yAxisTextWidth + YAxisAdjustment;
        var grid = yAxisCanvas.Parent as Grid;
        if (grid == null || grid.ColumnDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var axisColumn = grid.ColumnDefinitions[1]; 
        axisColumn.Width = new GridLength(_yAxisWidth);
    }

    private void OnXAxisCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var xAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        _xAxisHeight = _yAxisWidth;
        var grid = xAxisCanvas.Parent as Grid;
        if (grid == null || grid.RowDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var axisRow = grid.RowDefinitions[1];
        axisRow.Height = new GridLength(_xAxisHeight);
    }

    private float CalculateAxisCanvasWidth()
    {
        var textLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), _yAxisTextExample, _yAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        var textBounds = textLayout.LayoutBounds;
        return (float)textBounds.Width;
    }

    private void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearGraphCanvas(args);
        RenderData(args);
    }

    private void YAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearYAxisCanvas(args);
        RenderYAxis(args);
    }

    private void XAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearXAxisCanvas(args);
        RenderXAxis(args);
    }

    private void ClearGraphCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_graphBackgroundColor);
    }

    private void ClearYAxisCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_yxAxisBackgroundColor);
    }

    private void ClearXAxisCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_yxAxisBackgroundColor);
    }

    private void RenderData(CanvasDrawEventArgs args)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var ask = (float)_kernel[_horizontalKernelShiftInUnits].Ask;
        var highestPrice = ask + (_pipsPerChart * _pip) / 2f - _verticalShiftInPips * _pip;
        _data[_horizontalGraphShiftInUnits].Y = (highestPrice - ask) / _pip * _verticalScale;
        args.DrawingSession.DrawCircle(_data[_horizontalGraphShiftInUnits], 3, Colors.White);
        cpb.BeginFigure(_data[_horizontalGraphShiftInUnits]);

        for (var unit = 1; unit < _unitsPerChart; unit++)
        {
            if (unit + _horizontalGraphShiftInUnits >= _unitsPerChart) break;
            _data[unit + _horizontalGraphShiftInUnits].Y = (highestPrice - (float)_kernel[unit + _horizontalKernelShiftInUnits].Ask) / _pip * _verticalScale;
            args.DrawingSession.DrawCircle(_data[unit + _horizontalGraphShiftInUnits], 1, Colors.White);
            cpb.AddLine(_data[unit + _horizontalGraphShiftInUnits]);
        }

        cpb.EndFigure(CanvasFigureLoop.Open);
        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _graphForegroundColor, GraphDataStrokeThickness);
    }

    private void RenderYAxis(CanvasDrawEventArgs args)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var ask = (float)_kernel[_horizontalKernelShiftInUnits].Ask;
        var bid = (float)_kernel[_horizontalKernelShiftInUnits].Bid;

        var highestPrice = ask + (_pipsPerChart * _pip) / 2f - _verticalShiftInPips * _pip;
        var divisor = 1f / (YAxisStepInPips * _pip);
        var firstPriceDivisibleBy10Pips = (float)Math.Floor(highestPrice * divisor) / divisor;
        
        var y = (highestPrice - ask) / _pip * _verticalScale;
        cpb.BeginFigure(new Vector2(0, y));
        cpb.AddLine(new Vector2(_yAxisWidth, y));
        cpb.EndFigure(CanvasFigureLoop.Open);
        var textLayout = new CanvasTextLayout(args.DrawingSession, ask.ToString(_yxAxisLabelFormat), _yAxisTextFormat, _yAxisWidth, YAxisFontSize);
        args.DrawingSession.DrawTextLayout(textLayout, 0, y - YAxisFontSize - YAxisAdjustment, _yAxisAskBidForegroundColor);

        y = (highestPrice - bid) / _pip * _verticalScale;
        cpb.BeginFigure(new Vector2(0, y));
        cpb.AddLine(new Vector2(_yAxisWidth, y));
        cpb.EndFigure(CanvasFigureLoop.Open);
        textLayout = new CanvasTextLayout(args.DrawingSession, bid.ToString(_yxAxisLabelFormat), _yAxisTextFormat, _yAxisWidth, YAxisFontSize);
        args.DrawingSession.DrawTextLayout(textLayout, 0, y + YAxisAdjustment, _yAxisAskBidForegroundColor);

        for (var price = firstPriceDivisibleBy10Pips; price >= highestPrice - _pipsPerChart * _pip; price -= _pip * YAxisStepInPips)
        {
            y = (highestPrice - price) / _pip * _verticalScale;
            textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(_yxAxisLabelFormat), _yAxisTextFormat, _yAxisWidth, YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0, y - YAxisFontSize - YAxisAdjustment, _yAxisForegroundColor);

            cpb.BeginFigure(new Vector2(0, y));
            cpb.AddLine(new Vector2(_yAxisWidth, y));
            cpb.EndFigure(CanvasFigureLoop.Open);
        }

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _yAxisForegroundColor, 1);
    }

    private void RenderXAxis(CanvasDrawEventArgs args)
    {
        
    }

    private void OnGraphCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        _isMouseDown = true;
        _previousMouseY = (float)e.GetCurrentPoint(_graphCanvas).Position.Y;
        _previousMouseX = (float)e.GetCurrentPoint(_graphCanvas).Position.X;
        _graphCanvas!.CapturePointer(e.Pointer);
    }

    private void OnGraphCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isMouseDown)
        {
            return;
        }

        var currentMouseY = (float)e.GetCurrentPoint(_graphCanvas).Position.Y;
        var deltaY = _previousMouseY - currentMouseY;
        var pipsChange = deltaY / _verticalScale;

        var currentMouseX = (float)e.GetCurrentPoint(_graphCanvas).Position.X;
        var deltaX = _previousMouseX - currentMouseX;
        var barsChange = deltaX switch
        {
            > 0 => (int)Math.Floor(deltaX / _horizontalScale),
            < 0 => (int)Math.Ceiling(deltaX / _horizontalScale),
            _ => 0
        };

        if (Math.Abs(pipsChange) < 1 && Math.Abs(barsChange) < 1)
        {
            return;
        }

        _verticalShiftInPips += pipsChange;

        if (_horizontalGraphShiftInUnits == 0 && _horizontalKernelShiftInUnits == 0)
        {
            if (barsChange > 0)
            {
                _horizontalGraphShiftInUnits += barsChange;
                _horizontalGraphShiftInUnits = Math.Clamp(_horizontalGraphShiftInUnits, 0, _unitsPerChart - 1);
            }
            else if (barsChange < 0)
            {
                _horizontalKernelShiftInUnits -= barsChange;
                _horizontalKernelShiftInUnits = Math.Clamp(_horizontalKernelShiftInUnits, 0, _kernel.Count - _unitsPerChart);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        else if (_horizontalGraphShiftInUnits > 0)
        {
            Debug.Assert(_horizontalKernelShiftInUnits == 0);
            _horizontalGraphShiftInUnits += barsChange;
            _horizontalGraphShiftInUnits = Math.Clamp(_horizontalGraphShiftInUnits, 0, _unitsPerChart - 1);
        }
        else if (_horizontalKernelShiftInUnits > 0)
        {
            Debug.Assert(_horizontalGraphShiftInUnits == 0);
            _horizontalKernelShiftInUnits -= barsChange;
            _horizontalKernelShiftInUnits = Math.Clamp(_horizontalKernelShiftInUnits, 0, _kernel.Count - _unitsPerChart);
        }
        else
        {
            throw new InvalidOperationException();
        }

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();

        _previousMouseY = currentMouseY;
        _previousMouseX = currentMouseX;
    }

    private void OnGraphCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        _isMouseDown = false;
        _graphCanvas!.ReleasePointerCapture(e.Pointer);
    }

    private void OnYAxisCanvasPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void OnYAxisCanvasPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void OnYAxisCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = true;
        _previousMouseY = (float)e.GetCurrentPoint(_yAxisCanvas).Position.Y;
        _yAxisCanvas!.CapturePointer(e.Pointer);
    }

    private void OnYAxisCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isMouseDown)
        {
            return;
        }

        var currentMouseY = (float)e.GetCurrentPoint(_yAxisCanvas).Position.Y;
        var deltaY = _previousMouseY - currentMouseY;
        var pipsChange = deltaY / _verticalScale;

        if (Math.Abs(pipsChange) < 1)
        {
            return;
        }

        _pipsPerChart += pipsChange;
        _pipsPerChart = Math.Clamp(_pipsPerChart, 10, 200);
        _verticalScale = _height / (_pipsPerChart - 1);

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();

        _previousMouseY = currentMouseY;
    }

    private void OnYAxisCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = false;
        _yAxisCanvas!.ReleasePointerCapture(e.Pointer);
    }

    private void OnXAxisCanvasPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void OnXAxisCanvasPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void OnXAxisCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = true;
        _previousMouseX = (float)e.GetCurrentPoint(_xAxisCanvas).Position.X;
        _xAxisCanvas!.CapturePointer(e.Pointer);
    }

    private void OnXAxisCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isMouseDown)
        {
            return;
        }

        var currentMouseX = (float)e.GetCurrentPoint(_xAxisCanvas).Position.X;
        var deltaX = _previousMouseX - currentMouseX;
        var unitsChange = deltaX / _horizontalScale;

        if (Math.Abs(unitsChange) < 1)
        {
            return;
        }

        _pendingUnitsPerChart += unitsChange;
        _previousMouseX = currentMouseX;
    }

    private void OnXAxisCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _unitsPerChart -= (int)Math.Floor(_pendingUnitsPerChart);
        _unitsPerChart = Math.Clamp(_unitsPerChart, MinUnitsPerChart, MaxUnitsPerChart);
        _horizontalScale = _width / (_unitsPerChart - 1);

        _data = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _data[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }

        _isMouseDown = false;
        _xAxisCanvas!.ReleasePointerCapture(e.Pointer);
        _pendingUnitsPerChart = 0;

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();
    }

    public void Detach()
    {
        if (_graphCanvas != null)
        {
            _graphCanvas.SizeChanged -= OnGraphCanvasSizeChanged;
            _graphCanvas.Draw -= GraphCanvas_OnDraw;
            _graphCanvas.PointerPressed -= OnGraphCanvasPointerPressed;
            _graphCanvas.PointerMoved -= OnGraphCanvasPointerMoved;
            _graphCanvas.PointerReleased -= OnGraphCanvasPointerReleased;

        }

        if (_yAxisCanvas != null)
        {
            _yAxisCanvas.SizeChanged -= OnYAxisCanvasSizeChanged;
            _yAxisCanvas.Draw -= YAxisCanvasOnDraw;
            _yAxisCanvas.PointerEntered -= OnYAxisCanvasPointerEntered;
            _yAxisCanvas.PointerExited -= OnYAxisCanvasPointerExited;
            _yAxisCanvas.PointerPressed -= OnYAxisCanvasPointerPressed;
            _yAxisCanvas.PointerMoved -= OnYAxisCanvasPointerMoved;
            _yAxisCanvas.PointerReleased -= OnYAxisCanvasPointerReleased;
        }

        if (_xAxisCanvas != null)
        {
            _xAxisCanvas.SizeChanged -= OnXAxisCanvasSizeChanged;
            _xAxisCanvas.Draw -= XAxisCanvasOnDraw;
            _xAxisCanvas.PointerEntered -= OnXAxisCanvasPointerEntered;
            _xAxisCanvas.PointerExited -= OnXAxisCanvasPointerExited;
            _xAxisCanvas.PointerPressed -= OnXAxisCanvasPointerPressed;
            _xAxisCanvas.PointerMoved -= OnXAxisCanvasPointerMoved;
            _xAxisCanvas.PointerReleased -= OnXAxisCanvasPointerReleased;
        }
    }
}