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
using Microsoft.UI.Input;
using System.Diagnostics;
using Symbol = Common.Entities.Symbol;
using ColorCode.Compilation.Languages;
using Microsoft.UI.Xaml.Shapes;

namespace Terminal.WinUI3.Controls;

public class BaseChartControl : Control
{
    private CanvasControl? _graphCanvas;
    private CanvasControl? _yAxisCanvas;
    private CanvasControl? _xAxisCanvas;
    private CanvasControl? _debugCanvas;
    private readonly Kernel _kernel;
    private Vector2[] _askData = null!;
    private Vector2[] _bidData = null!;
    private readonly bool _isReversed;

    private readonly float _pip;
    private const float YAxisStepInPips = 10f; 
    private float _width;
    private float _yAxisWidth;
    private float _height;
    private float _xAxisHeight;
    private int _unitsPerChart = int.MaxValue; //axis X //todo: settings //todo:changed when size changed
    private float _pendingUnitsPerChart;
    private int _maxUnitsPerChart;
    private const int MinUnitsPerChart = 10; //todo: settings
    private float _pipsPerChart = 10; //axis Y //todo: settings
    private const int MaxPipsPerChart = 200; //todo: settings
    private const int MinPipsPerChart = 10; //todo: settings
    private float _horizontalScale;
    private float _verticalScale;

    private float _verticalShift; 
    private int _horizontalShift; 
    private int _kernelShift;

    private const float YAxisFontSize = 12;
    private const string YAxisFontFamily = "Lucida Console";
    private readonly CanvasTextFormat _yxAxisTextFormat = new() { FontSize = YAxisFontSize, FontFamily = YAxisFontFamily };
    private const string YAxisTextSample = "0.12345";
    private const string XAxisTextSample = "HH:mm:ss";

    private const float GraphDataStrokeThickness = 1;
    private readonly Color _graphBackgroundColor = Colors.Black;
    private readonly Color _graphForegroundColor = Colors.White;
    private readonly Color _yxAxisForegroundColor = Colors.Gray;
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

    private DebugInfo _debugInfo;

    private const float ArrowheadLength = 10;
    private const float ArrowheadWidth = 5;

    private readonly IList<(Vector2 startPoint, Vector2 endPoint)> _arrowLines = new List<(Vector2, Vector2)>()
    {
        new(new Vector2(10, 10), new Vector2(110, 10)),
        new(new Vector2(110, 10), new Vector2(110, 110)),
        new(new Vector2(110, 110), new Vector2(10, 110)),
        new(new Vector2(10, 110), new Vector2(10, 10)),
        new(new Vector2(10, 10), new Vector2(60, 60)),
        new(new Vector2(110, 10), new Vector2(60, 60)),
        new( new Vector2(110, 110), new Vector2(60, 60)),
        new(new Vector2(10, 110), new Vector2(60, 60))
    };
    
    public BaseChartControl(Kernel kernel, Symbol symbol, bool isReversed)
    {
        DefaultStyleKey = typeof(BaseChartControl);
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _isReversed = isReversed;
        switch (symbol)
        {
            case Symbol.EURGBP:
            case Symbol.EURUSD:
            case Symbol.GBPUSD:
                _pip = 0.0001f;
                _yxAxisLabelFormat = "f5";
                break;
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                _pip = 0.01f;
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
        _debugCanvas = GetTemplateChild("debugCanvas") as CanvasControl;

        if (_graphCanvas is null || _yAxisCanvas is null || _xAxisCanvas is null || _debugCanvas is null)
        {
            throw new InvalidOperationException("Canvas controls not found in the template.");
        }

        _graphCanvas.SizeChanged += GraphCanvas_OnSizeChanged;
        _graphCanvas.Draw += GraphCanvas_OnDraw;
        _graphCanvas.PointerPressed += GraphCanvas_OnPointerPressed;
        _graphCanvas.PointerMoved += GraphCanvas_OnPointerMoved;
        _graphCanvas.PointerReleased += GraphCanvas_OnPointerReleased;

        _yAxisCanvas.SizeChanged += YAxisCanvas_OnSizeChanged;
        _yAxisCanvas.Draw += YAxisCanvas_OnDraw;
        _yAxisCanvas.PointerEntered += YAxisCanvas_OnPointerEntered;
        _yAxisCanvas.PointerExited += YAxisCanvas_OnPointerExited;
        _yAxisCanvas.PointerPressed += YAxisCanvas_OnPointerPressed;
        _yAxisCanvas.PointerMoved += YAxisCanvas_OnPointerMoved;
        _yAxisCanvas.PointerReleased += YAxisCanvas_OnPointerReleased;

        _xAxisCanvas.SizeChanged += XAxisCanvas_OnSizeChanged;
        _xAxisCanvas.Draw += XAxisCanvas_OnDraw;
        _xAxisCanvas.PointerEntered += XAxisCanvas_OnPointerEntered;
        _xAxisCanvas.PointerExited += XAxisCanvas_OnPointerExited;
        _xAxisCanvas.PointerPressed += XAxisCanvas_OnPointerPressed;
        _xAxisCanvas.PointerMoved += XAxisCanvas_OnPointerMoved;
        _xAxisCanvas.PointerReleased += XAxisCanvas_OnPointerReleased;

        _debugCanvas.SizeChanged += DebugCanvas_OnSizeChanged;
        _debugCanvas.Draw += DebugCanvas_OnDraw;
    }

    private void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _width = (float)e.NewSize.Width;
        _maxUnitsPerChart = (int)Math.Floor(_width);
        _maxUnitsPerChart = Math.Clamp(_maxUnitsPerChart, MinUnitsPerChart, int.MaxValue);
        _unitsPerChart = Math.Clamp(_unitsPerChart, MinUnitsPerChart, _maxUnitsPerChart);
        _height = (float)e.NewSize.Height;
        _horizontalScale = _width / (_unitsPerChart - 1);
        _verticalScale = _height / _pipsPerChart;

        _askData = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _askData[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }
        _bidData = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _bidData[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }
    }

    private void YAxisCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var yAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        _yAxisWidth = CalculateTextBounds(YAxisTextSample, _yxAxisTextFormat).width;
        var grid = yAxisCanvas.Parent as Grid;
        if (grid == null || grid.ColumnDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var axisColumn = grid.ColumnDefinitions[1]; 
        axisColumn.Width = new GridLength(_yAxisWidth);
    }

    private void XAxisCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var xAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        _xAxisHeight = CalculateTextBounds(XAxisTextSample, _yxAxisTextFormat).height;
        var grid = xAxisCanvas.Parent as Grid;
        if (grid == null || grid.RowDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var axisRow = grid.RowDefinitions[1];
        axisRow.Height = new GridLength(_xAxisHeight);
    }

    private void DebugCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var debugCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        var grid = debugCanvas.Parent as Grid;
        if (grid == null || grid.RowDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }

        var debugRow = grid.RowDefinitions[2];
        debugRow.Height = new GridLength(CalculateTextBounds(YAxisTextSample, _yxAxisTextFormat).height * 2);//todo: 2 rows now
    }

    private void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearGraphCanvas(args);
        RenderData(args);
    }

    private void YAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearYAxisCanvas(args);
        RenderYAxis(args);
    }

    private void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearXAxisCanvas(args);
        RenderXAxis(args);
    }

    private void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        ClearDebugCanvas(args);
        RenderDebug(args);
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

    private void ClearDebugCanvas(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_graphBackgroundColor);
    }

    private void RenderData(CanvasDrawEventArgs args)
    {
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
            var offset = _isReversed ? _height : 0;

            var ask = (float)_kernel[_kernelShift].Ask;
            var yZeroPrice = ask + (_pipsPerChart * _pip) / 2f - _verticalShift * _pip;

            var unit = 0;
            _askData[_horizontalShift].Y = _isReversed ? offset - (yZeroPrice - ask) / _pip * _verticalScale : (yZeroPrice - ask) / _pip * _verticalScale;

            cpb.BeginFigure(_askData[_horizontalShift]);

            while (unit < _unitsPerChart - _horizontalShift)
            {
                _askData[unit + _horizontalShift].Y =
                    _isReversed ? offset - (yZeroPrice - (float)_kernel[unit + _kernelShift].Ask) / _pip * _verticalScale : (yZeroPrice - (float)_kernel[unit + _kernelShift].Ask) / _pip * _verticalScale;
                cpb.AddLine(_askData[unit + _horizontalShift]);
                unit++;
            }

            unit--;
            while (unit >= 0)
            {
                _bidData[unit + _horizontalShift].Y = _isReversed ? offset - (yZeroPrice - (float)_kernel[unit + _kernelShift].Bid) / _pip * _verticalScale : (yZeroPrice - (float)_kernel[unit + _kernelShift].Bid) / _pip * _verticalScale;
                cpb.AddLine(_bidData[unit + _horizontalShift]);
                unit--;
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
            foreach (var line in _arrowLines)
            {
                using var drawCpb = new CanvasPathBuilder(args.DrawingSession);
                var drawLineGeometry = GetDrawLineGeometry(drawCpb, line);
                dg.Add(drawLineGeometry);
                using var fillCpb = new CanvasPathBuilder(args.DrawingSession);
                var fillLineGeometry = GetFillLineGeometry(fillCpb, line);
                fg.Add(fillLineGeometry);
            }
        }

        static CanvasGeometry GetDrawLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            cpb.BeginFigure(line.startPoint);
            cpb.AddLine(line.endPoint);
            cpb.EndFigure(CanvasFigureLoop.Open);
            var drawLineGeometry = CanvasGeometry.CreatePath(cpb);
            return drawLineGeometry;
        }

        static CanvasGeometry GetFillLineGeometry(CanvasPathBuilder cpb, (Vector2 startPoint, Vector2 endPoint) line)
        {
            var (arrowHeadLeftPoint, arrowHeadRightPoint) = GetArrowPoints(line.endPoint, line.startPoint, ArrowheadLength, ArrowheadWidth);
            cpb.BeginFigure(line.endPoint);
            cpb.AddLine(arrowHeadLeftPoint);
            cpb.AddLine(arrowHeadRightPoint);
            cpb.AddLine(line.endPoint);
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
            args.DrawingSession.DrawGeometry(combinedDrawGeometry, _graphForegroundColor, GraphDataStrokeThickness);
            args.DrawingSession.FillGeometry(combinedFillGeometry, _graphForegroundColor);
        }
    }

    private void RenderYAxis(CanvasDrawEventArgs args)
    {
        var offset = _isReversed ? _height : 0;

        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var ask = (float)_kernel[_kernelShift].Ask; 
        var yZeroPrice = ask + (_pip * _pipsPerChart) / 2f - _verticalShift * _pip;
        var y = Math.Abs(offset - (yZeroPrice - ask) / _pip * _verticalScale);
        cpb.BeginFigure(new Vector2(0, y));
        cpb.AddLine(new Vector2(_yAxisWidth, y));
        cpb.EndFigure(CanvasFigureLoop.Open);
        var textLayout = new CanvasTextLayout(args.DrawingSession, ask.ToString(_yxAxisLabelFormat), _yxAxisTextFormat, _yAxisWidth, YAxisFontSize);
        args.DrawingSession.DrawTextLayout(textLayout, 0, y - (float)textLayout.LayoutBounds.Height, _yAxisAskBidForegroundColor);

        var bid = (float)_kernel[_kernelShift].Bid;
        y = Math.Abs(offset - (yZeroPrice - bid) / _pip * _verticalScale);
        cpb.BeginFigure(new Vector2(0, y));
        cpb.AddLine(new Vector2(_yAxisWidth, y));
        cpb.EndFigure(CanvasFigureLoop.Open);
        textLayout = new CanvasTextLayout(args.DrawingSession, bid.ToString(_yxAxisLabelFormat), _yxAxisTextFormat, _yAxisWidth, YAxisFontSize);
        args.DrawingSession.DrawTextLayout(textLayout, 0, y, _yAxisAskBidForegroundColor);

        var divisor = 1f / (YAxisStepInPips * _pip);
        var firstPriceDivisibleBy10Pips = (float)Math.Floor(yZeroPrice * divisor) / divisor;

        for (var price = firstPriceDivisibleBy10Pips; price >= yZeroPrice - _pipsPerChart * _pip; price -= _pip * YAxisStepInPips)
        {
            y = Math.Abs(offset - (yZeroPrice - price) / _pip * _verticalScale);
            textLayout = new CanvasTextLayout(args.DrawingSession, price.ToString(_yxAxisLabelFormat), _yxAxisTextFormat, _yAxisWidth, YAxisFontSize);
            args.DrawingSession.DrawTextLayout(textLayout, 0, y - (float)textLayout.LayoutBounds.Height, _yxAxisForegroundColor);

            cpb.BeginFigure(new Vector2(0, y));
            cpb.AddLine(new Vector2(_yAxisWidth, y));
            cpb.EndFigure(CanvasFigureLoop.Open);
        }

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _yxAxisForegroundColor, 1);
    }

    private void RenderXAxis(CanvasDrawEventArgs args)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        
        var startUnit = _kernelShift;
        var endUnit = _unitsPerChart - _horizontalShift + _kernelShift - 1;
        var totalUnits = endUnit - startUnit + 1;

        var endTime = _kernel[startUnit].DateTime;
        var startTime = _kernel[endUnit].DateTime;//todo
        var timeSpan = endTime - startTime;
        var totalSeconds = timeSpan.TotalSeconds;
        if (totalSeconds <= 0)
        {
            return;
        }

        var pixelsPerSecond = _width / totalSeconds;

        _debugInfo.StartTime = startTime;
        _debugInfo.EndTime = endTime;
        _debugInfo.TimeSpan = timeSpan;
        
        var minTicks = 2;
        var maxTicks = 20;
        var minTimeStep = totalSeconds / maxTicks; 
        var maxTimeStep = totalSeconds / minTicks;

        var timeSteps = new List<double> { 1, 5, 10, 30, 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60 };
        var timeStep = timeSteps.First(t => t >= minTimeStep);
        if (timeStep > maxTimeStep)
        {
            timeStep = maxTimeStep;
        }

        startTime = RoundDateTime(startTime, timeStep);
        _debugInfo.TimeStep = timeStep;
        _debugInfo.NewStartTime = startTime;

        for (double tickTime = 0; tickTime <= totalSeconds; tickTime += timeStep)
        {
            var tickDateTime = startTime.AddSeconds(tickTime);
            var x = (float)(tickTime * pixelsPerSecond);

            cpb.BeginFigure(new Vector2(x, 0));
            cpb.AddLine(new Vector2(x, _xAxisHeight));
            cpb.EndFigure(CanvasFigureLoop.Open);

            var textLayout = new CanvasTextLayout(args.DrawingSession, $"{tickDateTime:t}", _yxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            args.DrawingSession.DrawTextLayout(textLayout, x + 3, 0, _yxAxisForegroundColor);
        }

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), _yxAxisForegroundColor, 1);
    }

    private void RenderDebug(CanvasDrawEventArgs args)
    {
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        var output1 = $"width:{_width:0000}, height:{_height:0000}, units:{_unitsPerChart:0000}, pips:{_pipsPerChart:0000}, vertical shift:{_verticalShift:###,##}, horizontal shift:{_horizontalShift:0000}, kernel shift:{_kernelShift:000000}, kernel.Count:{_kernel.Count:000000}";
        var output2 = $"Start Time:{_debugInfo.StartTime}, End Time:{_debugInfo.EndTime}, Time Span:{_debugInfo.TimeSpan:g}, Time Step:{_debugInfo.TimeStep}, New Start Time:{_debugInfo.NewStartTime}";

        var textLayout1 = new CanvasTextLayout(args.DrawingSession, output1, _yxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        args.DrawingSession.DrawTextLayout(textLayout1, 0, 0, Colors.White);

        var textLayout2 = new CanvasTextLayout(args.DrawingSession, output2, _yxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
        args.DrawingSession.DrawTextLayout(textLayout2, 0, (float)textLayout1.LayoutBounds.Height, Colors.White);
    }

    private void GraphCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        _isMouseDown = true;
        _previousMouseY = (float)e.GetCurrentPoint(_graphCanvas).Position.Y;
        _previousMouseX = (float)e.GetCurrentPoint(_graphCanvas).Position.X;
        _graphCanvas!.CapturePointer(e.Pointer);
    }

    private void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
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

        _verticalShift += pipsChange;

        if (_horizontalShift == 0 && _kernelShift == 0)
        {
            if (barsChange > 0)
            {
                _horizontalShift += barsChange;
                _horizontalShift = Math.Clamp(_horizontalShift, 0, _unitsPerChart - 1);
            }
            else if (barsChange < 0)
            {
                _kernelShift -= barsChange;
                _kernelShift = Math.Clamp(_kernelShift, 0, _kernel.Count - _unitsPerChart);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        else if (_horizontalShift > 0)
        {
            Debug.Assert(_kernelShift == 0);
            _horizontalShift += barsChange;
            _horizontalShift = Math.Clamp(_horizontalShift, 0, _unitsPerChart - 1);
        }
        else if (_kernelShift > 0)
        {
            Debug.Assert(_horizontalShift == 0);
            _kernelShift -= barsChange;
            _kernelShift = Math.Clamp(_kernelShift, 0, _kernel.Count - _unitsPerChart);
        }
        else
        {
            throw new InvalidOperationException();
        }

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();
        _debugCanvas!.Invalidate();

        _previousMouseY = currentMouseY;
        _previousMouseX = currentMouseX;
    }

    private void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        _isMouseDown = false;
        _graphCanvas!.ReleasePointerCapture(e.Pointer);
    }

    private void YAxisCanvas_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void YAxisCanvas_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void YAxisCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = true;
        _previousMouseY = (float)e.GetCurrentPoint(_yAxisCanvas).Position.Y;
        _yAxisCanvas!.CapturePointer(e.Pointer);
    }

    private void YAxisCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
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
        _pipsPerChart = Math.Clamp(_pipsPerChart, MinPipsPerChart, MaxPipsPerChart);
        _verticalScale = _height / (_pipsPerChart - 1);

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();
        _debugCanvas!.Invalidate();

        _previousMouseY = currentMouseY;
    }

    private void YAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = false;
        _yAxisCanvas!.ReleasePointerCapture(e.Pointer);
    }

    private void XAxisCanvas_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void XAxisCanvas_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void XAxisCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = true;
        _previousMouseX = (float)e.GetCurrentPoint(_xAxisCanvas).Position.X;
        _xAxisCanvas!.CapturePointer(e.Pointer);
    }

    private void XAxisCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
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

    private void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _unitsPerChart -= (int)Math.Floor(_pendingUnitsPerChart);
        _unitsPerChart = Math.Clamp(_unitsPerChart, MinUnitsPerChart, _maxUnitsPerChart);
        _horizontalScale = _width / (_unitsPerChart - 1);

        _askData = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _askData[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }

        _bidData = new Vector2[_unitsPerChart];
        for (var unit = 0; unit < _unitsPerChart; unit++)
        {
            _bidData[unit] = new Vector2 { X = (_unitsPerChart - 1 - unit) * _horizontalScale };
        }

        _isMouseDown = false;
        _xAxisCanvas!.ReleasePointerCapture(e.Pointer);
        _pendingUnitsPerChart = 0;

        _graphCanvas!.Invalidate();
        _yAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();
        _debugCanvas!.Invalidate();
    }

    public void Detach()
    {
        if (_graphCanvas != null)
        {
            _graphCanvas.SizeChanged -= GraphCanvas_OnSizeChanged;
            _graphCanvas.Draw -= GraphCanvas_OnDraw;
            _graphCanvas.PointerPressed -= GraphCanvas_OnPointerPressed;
            _graphCanvas.PointerMoved -= GraphCanvas_OnPointerMoved;
            _graphCanvas.PointerReleased -= GraphCanvas_OnPointerReleased;

        }

        if (_yAxisCanvas != null)
        {
            _yAxisCanvas.SizeChanged -= YAxisCanvas_OnSizeChanged;
            _yAxisCanvas.Draw -= YAxisCanvas_OnDraw;
            _yAxisCanvas.PointerEntered -= YAxisCanvas_OnPointerEntered;
            _yAxisCanvas.PointerExited -= YAxisCanvas_OnPointerExited;
            _yAxisCanvas.PointerPressed -= YAxisCanvas_OnPointerPressed;
            _yAxisCanvas.PointerMoved -= YAxisCanvas_OnPointerMoved;
            _yAxisCanvas.PointerReleased -= YAxisCanvas_OnPointerReleased;
        }

        if (_xAxisCanvas != null)
        {
            _xAxisCanvas.SizeChanged -= XAxisCanvas_OnSizeChanged;
            _xAxisCanvas.Draw -= XAxisCanvas_OnDraw;
            _xAxisCanvas.PointerEntered -= XAxisCanvas_OnPointerEntered;
            _xAxisCanvas.PointerExited -= XAxisCanvas_OnPointerExited;
            _xAxisCanvas.PointerPressed -= XAxisCanvas_OnPointerPressed;
            _xAxisCanvas.PointerMoved -= XAxisCanvas_OnPointerMoved;
            _xAxisCanvas.PointerReleased -= XAxisCanvas_OnPointerReleased;
        }

        if (_debugCanvas != null)
        {
            _debugCanvas.SizeChanged -= DebugCanvas_OnSizeChanged;
            _debugCanvas.Draw -= DebugCanvas_OnDraw;

        }
    }

    private static (float width, float height) CalculateTextBounds(string textSample, CanvasTextFormat textFormat)
    {
        var textLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), textSample, textFormat, float.PositiveInfinity, float.PositiveInfinity);
        var textBounds = textLayout.LayoutBounds;
        return ((float)textBounds.Width, (float)textBounds.Height);
    }

    private static DateTime RoundDateTime(DateTime dt, double timeStep)
    {
        dt = dt.AddMilliseconds(-dt.TimeOfDay.Milliseconds);
        var totalSeconds = dt.TimeOfDay.TotalSeconds;
        var modulo = totalSeconds % timeStep;
        return modulo < timeStep / 2 ? dt.AddSeconds(-modulo) : dt.AddSeconds(timeStep - modulo);
    }

    private struct DebugInfo
    {
        public DateTime StartTime
        {
            get;
            set;
        }

        public DateTime EndTime
        {
            get;
            set;
        }

        public TimeSpan TimeSpan
        {
            get;
            set;
        }

        public double TimeStep
        {
            get;
            set;
        }

        public DateTime NewStartTime
        {
            get;
            set;
        }
    }
}