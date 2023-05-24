/*+------------------------------------------------------------------+
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

namespace Terminal.WinUI3.Controls;

public class BaseChartControl : Control
{
    private CanvasControl? _graphCanvas;
    private CanvasControl? _yAxisCanvas;
    private readonly Kernel _kernel;
    private readonly Vector2[] _data;

    private const float Pip = 0.0001f;
    private const float PipsAxisStep = 10f;
    private float _graphWidth;
    private float _yAxisWidth;
    private float _height;
    private const int UnitsPerChart = 2000; //axis X
    private int _pipsPerChart = 100; //axis Y
    private float _graphHorizontalScale;
    private float _verticalScale;

    private readonly int _horizontalShift = 0;
    private int _verticalShift = 0;
    private int _unitsShift = 0;

    private new const float YAxisFontSize = 12;
    private new const string YAxisFontFamily = "Lucida Console";
    private readonly CanvasTextFormat _yAxisTextFormat = new() { FontSize = YAxisFontSize, FontFamily = YAxisFontFamily };
    private const string YAxisTextExample = "1.2345";
    private const float YAxisAdjustment = 3;

    private const float GraphDataStrokeThickness = 1;
    private readonly Color _graphBackgroundColor = Colors.Black;
    private readonly Color _graphForegroundColor = Colors.White;
    private readonly Color _yAxisForegroundColor = Colors.White;
    private const string HexCode = "#202020"; // Raisin Black color
    private readonly  Color _yAxisBackgroundColor = Color.FromArgb(
        255,
        Convert.ToByte(HexCode.Substring(1, 2), 16),
        Convert.ToByte(HexCode.Substring(3, 2), 16),
        Convert.ToByte(HexCode.Substring(5, 2), 16)
    );

    private bool _isMouseDown;
    private float _previousMouseY;

    public BaseChartControl(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _data = new Vector2[UnitsPerChart];
        DefaultStyleKey = typeof(BaseChartControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _graphCanvas = GetTemplateChild("graphCanvas") as CanvasControl;
        _yAxisCanvas = GetTemplateChild("yAxisCanvas") as CanvasControl;

        if (_graphCanvas == null || _yAxisCanvas == null)
        {
            throw new InvalidOperationException("Canvas controls not found in the template.");
        }

        _graphCanvas.SizeChanged += OnGraphCanvasSizeChanged;
        _graphCanvas.Draw += GraphCanvas_OnDraw;

        _yAxisCanvas.SizeChanged += OnYAxisCanvasSizeChanged;
        _yAxisCanvas.Draw += YAxisCanvasOnDraw;
        _yAxisCanvas.PointerPressed += OnYAxisCanvasPointerPressed;
        _yAxisCanvas.PointerMoved += OnYAxisCanvasPointerMoved;
        _yAxisCanvas.PointerReleased += OnYAxisCanvasPointerReleased;
    }

    private void OnGraphCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _graphWidth = (float)e.NewSize.Width;
        _height = (float)e.NewSize.Height;
        _graphHorizontalScale = _graphWidth / (UnitsPerChart - 1);
        _verticalScale = _height / (_pipsPerChart - 1);

        for (var unit = 0; unit < UnitsPerChart; unit++)
        {
            _data[unit] = new Vector2 { X = (UnitsPerChart - 1 - unit - _horizontalShift) * _graphHorizontalScale };
        }
    }

    private void OnYAxisCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var yAxisCanvas = sender as CanvasControl;
        if (yAxisCanvas == null)
        {
            throw new InvalidOperationException("Canvas controls not found.");
        }

        _yAxisWidth = CalculateAxisCanvasWidth();
        var grid = yAxisCanvas.Parent as Grid;
        if (grid == null || grid.ColumnDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var axisColumn = grid.ColumnDefinitions[1]; 
        axisColumn.Width = new GridLength(_yAxisWidth);
    }

    private float CalculateAxisCanvasWidth()
    {
        var textLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), YAxisTextExample, _yAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        var textBounds = textLayout.LayoutBounds;
        return (float)textBounds.Width + YAxisAdjustment;
    }

    private void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearGraphCanvas(args);
        RenderData(args);
    }

    private void YAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearYAxisCanvas(args);
        RenderAxes(args);
    }

    private void ClearGraphCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_graphBackgroundColor);
    }

    private void ClearYAxisCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_yAxisBackgroundColor);
    }

    private void RenderData(CanvasDrawEventArgs args)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var highestPrice = (float)_kernel[0].Ask + (_pipsPerChart * Pip) / 2f + _verticalShift;
        _data[0].Y = (highestPrice - (float)_kernel[0].Ask) / Pip * _verticalScale;

        cpb.BeginFigure(_data[0]);

        for (var unit = 1; unit < UnitsPerChart; unit++)
        {
            _data[unit].Y = (highestPrice - (float)_kernel[unit].Ask) / Pip * _verticalScale;
            cpb.AddLine(_data[unit]);
        }

        cpb.EndFigure(CanvasFigureLoop.Open);
        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _graphForegroundColor, GraphDataStrokeThickness);
    }

    private void RenderAxes(CanvasDrawEventArgs args)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var ask = (float)_kernel[0].Ask;
        var highestPrice = ask + (_pipsPerChart * Pip) / 2f + _verticalShift;
        const float divisor = 1f / (PipsAxisStep * Pip);
        var firstPriceDivisibleBy10Pips = (float)Math.Floor(highestPrice * divisor) / divisor;
        
        cpb.BeginFigure(new Vector2(0, (highestPrice - ask) / Pip * _verticalScale));
        cpb.AddLine(new Vector2(_yAxisWidth, (highestPrice - ask) / Pip * _verticalScale));
        cpb.EndFigure(CanvasFigureLoop.Open);

        for (var price = firstPriceDivisibleBy10Pips; price >= highestPrice - _pipsPerChart * Pip; price -= Pip * PipsAxisStep)
        {
            var y = (highestPrice - price) / Pip * _verticalScale;
            var textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString("F4"), _yAxisTextFormat, _yAxisWidth, YAxisFontSize);
            var textWidth = textLayout.DrawBounds.Width;
           
            args.DrawingSession.DrawTextLayout(textLayout, _yAxisWidth - (float)textWidth - YAxisAdjustment, y - YAxisFontSize - YAxisAdjustment, _yAxisForegroundColor);

            cpb.BeginFigure(new Vector2(0, y));
            cpb.AddLine(new Vector2(_yAxisWidth, y));
            cpb.EndFigure(CanvasFigureLoop.Open);
        }

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _yAxisForegroundColor, 1);

        var pipsTextLayout = new CanvasTextLayout(args.DrawingSession, $"PipsPerChart: {_pipsPerChart}", _yAxisTextFormat, _yAxisWidth, YAxisFontSize);
        var pipsTextWidth = pipsTextLayout.DrawBounds.Width;
        args.DrawingSession.DrawTextLayout(pipsTextLayout, _yAxisWidth - (float)pipsTextWidth - YAxisAdjustment, YAxisAdjustment, _yAxisForegroundColor);
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
        var pipsChange = (int)Math.Floor(deltaY / _verticalScale);

        if (pipsChange == 0)
        {
            return;
        }

        _pipsPerChart += pipsChange;
        _pipsPerChart = Math.Clamp(_pipsPerChart, 10, 200);

        _verticalScale = _height / (_pipsPerChart - 1);

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();

        _previousMouseY = currentMouseY;
    }

    private void OnYAxisCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = false;
        _yAxisCanvas!.ReleasePointerCapture(e.Pointer);
    }

    public void Detach()
    {
        if (_graphCanvas != null)
        {
            _graphCanvas.SizeChanged -= OnGraphCanvasSizeChanged;
            _graphCanvas.Draw -= GraphCanvas_OnDraw;
        }
        
        if (_yAxisCanvas != null)
        {
            _yAxisCanvas.SizeChanged -= OnYAxisCanvasSizeChanged;
            _yAxisCanvas.Draw -= YAxisCanvasOnDraw;
            _yAxisCanvas.PointerPressed -= OnYAxisCanvasPointerPressed;
            _yAxisCanvas.PointerMoved -= OnYAxisCanvasPointerMoved;
            _yAxisCanvas.PointerReleased -= OnYAxisCanvasPointerReleased;
        }
    }
}