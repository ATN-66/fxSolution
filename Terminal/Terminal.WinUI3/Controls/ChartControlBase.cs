/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              ChartControlBase.cs |
  +------------------------------------------------------------------+*/

#define DEBUGWIN2DCanvasControl

using System.Diagnostics;
using System.Numerics;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls;

public abstract class ChartControlBase : Control
{
    protected const float YAxisStepInPips = 10f;
    protected const float YAxisFontSize = 12;
    private const string YAxisFontFamily = "Lucida Console";
    private const string YAxisTextSample = "0.12345";
    private const string XAxisTextSample = "HH:mm:ss";
    protected const float GraphDataStrokeThickness = 1;
    private const string HexCode = "#202020"; // Raisin Black color
    protected const int MinTicks = 2;
    protected const int MaxTicks = 20;
    protected const float ArrowheadLength = 10;
    protected const float ArrowheadWidth = 5;

    public static readonly DependencyProperty MaxUnitsPerChartProperty = DependencyProperty.Register(nameof(MaxUnitsPerChart), typeof(int), typeof(ChartControlBase), new PropertyMetadata(500));
    public static readonly DependencyProperty MinUnitsPerChartProperty = DependencyProperty.Register(nameof(MinUnitsPerChart), typeof(int), typeof(TickChartControl), new PropertyMetadata(10));
    public static readonly DependencyProperty UnitsPerChartProperty = DependencyProperty.Register(nameof(UnitsPerChart), typeof(int), typeof(TickChartControl), new PropertyMetadata(100));
    public static readonly DependencyProperty PipsPerChartProperty = DependencyProperty.Register(nameof(PipsPerChart), typeof(float), typeof(TickChartControl), new PropertyMetadata(10f));
    public static readonly DependencyProperty MaxPipsPerChartProperty = DependencyProperty.Register(nameof(MaxPipsPerChart), typeof(float), typeof(TickChartControl), new PropertyMetadata(200f));
    public static readonly DependencyProperty MinPipsPerChartProperty = DependencyProperty.Register(nameof(MinPipsPerChart), typeof(float), typeof(TickChartControl), new PropertyMetadata(10f));
    public static readonly DependencyProperty KernelShiftPercentProperty = DependencyProperty.Register(nameof(KernelShiftPercent), typeof(double), typeof(TickChartControl), new PropertyMetadata(0d));

    protected readonly IList<(Vector2 startPoint, Vector2 endPoint)> ArrowLines = new List<(Vector2, Vector2)>
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

    private readonly CanvasTextFormat _currencyFormat = new() { FontSize = 20, WordWrapping = CanvasWordWrapping.NoWrap };
    protected readonly Color GraphBackgroundColor = Colors.Transparent;
    protected readonly Color GraphForegroundColor = Colors.White;
    private readonly Color _textBackgroundColor = Colors.Black;
    protected readonly Color YAxisAskBidForegroundColor = Colors.White;
    protected readonly Color YxAxisBackgroundColor = Color.FromArgb(255, Convert.ToByte(HexCode.Substring(1, 2), 16), Convert.ToByte(HexCode.Substring(3, 2), 16), Convert.ToByte(HexCode.Substring(5, 2), 16));
    protected readonly Color YxAxisForegroundColor = Colors.Gray;
    protected readonly string YxAxisLabelFormat;
    protected readonly CanvasTextFormat YxAxisTextFormat = new() { FontSize = YAxisFontSize, FontFamily = YAxisFontFamily };
    protected readonly bool IsReversed;
    protected readonly ILogger<ChartControlBase> Logger;
    protected readonly float Pip;
    protected float GraphHeight;
    protected bool IsMouseDown;
    private float _previousMouseX;
    private float _previousMouseY;
    private CanvasControl? _textCanvas;
    protected bool EnableDrawing = true;
    protected CanvasControl? GraphCanvas;
    protected float HorizontalScale;
    protected int HorizontalShift;
    protected float PendingUnitsPerChart;
    protected float VerticalScale;
    protected float VerticalShift;
    protected CanvasControl? XAxisCanvas;
    protected float XAxisHeight;
    protected CanvasControl? YAxisCanvas;
    protected float YAxisWidth;

    protected ChartControlBase(Symbol symbol, bool isReversed, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger)
    {
        Logger = logger;

        IsReversed = isReversed;
        switch (symbol)
        {
            case Symbol.EURGBP:
            case Symbol.EURUSD:
            case Symbol.GBPUSD:
                Pip = 0.0001f;
                YxAxisLabelFormat = "f5";
                break;
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                Pip = 0.01f;
                YxAxisLabelFormat = "f3";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
        }

        BaseColor = baseColor;
        QuoteColor = quoteColor;
        (BaseCurrency, QuoteCurrency) = GetCurrenciesFromSymbol(symbol);
    }

    public int MaxUnitsPerChart
    {
        get => (int)GetValue(MaxUnitsPerChartProperty);
        set
        {
            if (MaxUnitsPerChart == value)
            {
                return;
            }

            SetValue(MaxUnitsPerChartProperty, value);
        }
    }

    public int MinUnitsPerChart
    {
        get => (int)GetValue(MinUnitsPerChartProperty);
        set
        {
            if (MinUnitsPerChart == value)
            {
                return;
            }

            SetValue(MinUnitsPerChartProperty, value);
        }
    }

    public int UnitsPerChart
    {
        get => (int)GetValue(UnitsPerChartProperty);
        set
        {
            if (UnitsPerChart == value)
            {
                return;
            }

            SetValue(UnitsPerChartProperty, value);
            OnUnitsPerChartChanged();
        }
    }

    public float PipsPerChart
    {
        get => (float)GetValue(PipsPerChartProperty);
        set
        {
            if (value.Equals(PipsPerChart))
            {
                return;
            }

            SetValue(PipsPerChartProperty, value);
            OnPipsPerChartChanged();
        }
    }

    public float MaxPipsPerChart
    {
        get => (float)GetValue(MaxPipsPerChartProperty);
        set => SetValue(MaxPipsPerChartProperty, value);
    }

    public float MinPipsPerChart
    {
        get => (float)GetValue(MinPipsPerChartProperty);
        set => SetValue(MinPipsPerChartProperty, value);
    }

    public double KernelShiftPercent
    {
        get => (double)GetValue(KernelShiftPercentProperty);
        set
        {
            if (value.Equals(KernelShiftPercent))
            {
                return;
            }

            value = AdjustRangeOnKernelShiftPercent(value);

            SetValue(KernelShiftPercentProperty, value);
            if (EnableDrawing)
            {
                OnKernelShiftPercentChanged(value);
            }
        }
    }

    public string BaseCurrency
    {
        get;
        set;
    }

    public string QuoteCurrency
    {
        get;
        set;
    }

    protected Color BaseColor
    {
        get;
        set;
    }

    protected Color QuoteColor
    {
        get;
        set;
    }

    protected abstract void OnUnitsPerChartChanged();
    private void OnPipsPerChartChanged()
    {
        try
        {
            VerticalScale = GraphHeight / PipsPerChart;
            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
            DebugCanvas!.Invalidate();
#endif
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "OnPipsPerChartChanged");
            throw;
        }
    }
    protected abstract double AdjustRangeOnKernelShiftPercent(double value);
    protected abstract void OnKernelShiftPercentChanged(double value);

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        GraphCanvas = GetTemplateChild("graphCanvas") as CanvasControl;
        _textCanvas = GetTemplateChild("textCanvas") as CanvasControl;
        YAxisCanvas = GetTemplateChild("yAxisCanvas") as CanvasControl;
        XAxisCanvas = GetTemplateChild("xAxisCanvas") as CanvasControl;
#if DEBUGWIN2DCanvasControl
        DebugCanvas = GetTemplateChild("debugCanvas") as CanvasControl;
        if (GraphCanvas is null || YAxisCanvas is null || XAxisCanvas is null || _textCanvas is null || DebugCanvas is null)
#else
        if (_graphCanvas is null || _textCanvas is null || _yAxisCanvas is null || _xAxisCanvas is null)
#endif

        {
            throw new InvalidOperationException("Canvas controls not found in the template.");
        }

        GraphCanvas.SizeChanged += GraphCanvas_OnSizeChanged;
        GraphCanvas.Draw += GraphCanvas_OnDraw;
        GraphCanvas.PointerPressed += GraphCanvas_OnPointerPressed;
        GraphCanvas.PointerMoved += GraphCanvas_OnPointerMoved;
        GraphCanvas.PointerReleased += GraphCanvas_OnPointerReleased;

        _textCanvas.SizeChanged += TextCanvas_OnSizeChanged;
        _textCanvas.Draw += TextCanvas_OnDraw;

        YAxisCanvas.SizeChanged += YAxisCanvas_OnSizeChanged;
        YAxisCanvas.Draw += YAxisCanvas_OnDraw;
        YAxisCanvas.PointerEntered += YAxisCanvas_OnPointerEntered;
        YAxisCanvas.PointerExited += YAxisCanvas_OnPointerExited;
        YAxisCanvas.PointerPressed += YAxisCanvas_OnPointerPressed;
        YAxisCanvas.PointerMoved += YAxisCanvas_OnPointerMoved;
        YAxisCanvas.PointerReleased += YAxisCanvas_OnPointerReleased;

        //XAxisCanvas.SizeChanged += XAxisCanvas_OnSizeChanged;
        //XAxisCanvas.Draw += XAxisCanvas_OnDraw;
        //XAxisCanvas.PointerEntered += XAxisCanvas_OnPointerEntered;
        //XAxisCanvas.PointerExited += XAxisCanvas_OnPointerExited;
        //XAxisCanvas.PointerPressed += XAxisCanvas_OnPointerPressed;
        //XAxisCanvas.PointerMoved += XAxisCanvas_OnPointerMoved;
        //XAxisCanvas.PointerReleased += XAxisCanvas_OnPointerReleased;
#if DEBUGWIN2DCanvasControl
        DebugCanvas.SizeChanged += DebugCanvas_OnSizeChanged;
        DebugCanvas.Draw += DebugCanvas_OnDraw;
#endif
    }
    public void Detach()
    {
        try
        {
            if (GraphCanvas != null)
            {
                GraphCanvas.SizeChanged -= GraphCanvas_OnSizeChanged;
                GraphCanvas.Draw -= GraphCanvas_OnDraw;
                GraphCanvas.PointerPressed -= GraphCanvas_OnPointerPressed;
                GraphCanvas.PointerMoved -= GraphCanvas_OnPointerMoved;
                GraphCanvas.PointerReleased -= GraphCanvas_OnPointerReleased;
            }

            if (_textCanvas != null)
            {
                _textCanvas.SizeChanged -= TextCanvas_OnSizeChanged;
                _textCanvas.Draw -= TextCanvas_OnDraw;
            }

            if (YAxisCanvas != null)
            {
                YAxisCanvas.SizeChanged -= YAxisCanvas_OnSizeChanged;
                YAxisCanvas.Draw -= YAxisCanvas_OnDraw;
                YAxisCanvas.PointerEntered -= YAxisCanvas_OnPointerEntered;
                YAxisCanvas.PointerExited -= YAxisCanvas_OnPointerExited;
                YAxisCanvas.PointerPressed -= YAxisCanvas_OnPointerPressed;
                YAxisCanvas.PointerMoved -= YAxisCanvas_OnPointerMoved;
                YAxisCanvas.PointerReleased -= YAxisCanvas_OnPointerReleased;
            }

            if (XAxisCanvas != null)
            {
                XAxisCanvas.SizeChanged -= XAxisCanvas_OnSizeChanged;
                XAxisCanvas.Draw -= XAxisCanvas_OnDraw;
                XAxisCanvas.PointerEntered -= XAxisCanvas_OnPointerEntered;
                XAxisCanvas.PointerExited -= XAxisCanvas_OnPointerExited;
                XAxisCanvas.PointerPressed -= XAxisCanvas_OnPointerPressed;
                XAxisCanvas.PointerMoved -= XAxisCanvas_OnPointerMoved;
                XAxisCanvas.PointerReleased -= XAxisCanvas_OnPointerReleased;
            }
#if DEBUGWIN2DCanvasControl
            if (DebugCanvas != null)
            {
                DebugCanvas.SizeChanged -= DebugCanvas_OnSizeChanged;
                DebugCanvas.Draw -= DebugCanvas_OnDraw;
            }
#endif
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "Detach");
            throw;
        }
    }

    protected abstract void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e);
    protected abstract void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);

    private void GraphCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            IsMouseDown = true;
            _previousMouseY = (float)e.GetCurrentPoint(GraphCanvas).Position.Y;
            _previousMouseX = (float)e.GetCurrentPoint(GraphCanvas).Position.X;
            GraphCanvas!.CapturePointer(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnPointerPressed");
            throw;
        }
    }

    private void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (!IsMouseDown)
            {
                return;
            }

            var currentMouseY = (float)e.GetCurrentPoint(GraphCanvas).Position.Y;


            var deltaY = _previousMouseY - currentMouseY;
            deltaY = IsReversed ? -deltaY : deltaY;

            var pipsChange = deltaY / VerticalScale;

            var currentMouseX = (float)e.GetCurrentPoint(GraphCanvas).Position.X;
            var deltaX = _previousMouseX - currentMouseX;
            var barsChange = deltaX switch
            {
                > 0 => (int)Math.Floor(deltaX / HorizontalScale),
                < 0 => (int)Math.Ceiling(deltaX / HorizontalScale),
                _ => 0
            };

            if (Math.Abs(pipsChange) < 1 && Math.Abs(barsChange) < 1)
            {
                return;
            }

            VerticalShift += pipsChange;

            switch (HorizontalShift)
            {
                case 0 when KernelShiftValue == 0:
                    switch (barsChange)
                    {
                        case > 0:
                            HorizontalShift += barsChange;
                            HorizontalShift = Math.Clamp(HorizontalShift, 0, UnitsPerChart - 1);
                            break;
                        case < 0:
                            KernelShift = KernelShiftValue - barsChange;
                            break;
                    }

                    break;
                case > 0:
                    Debug.Assert(KernelShiftValue == 0);
                    HorizontalShift += barsChange;
                    HorizontalShift = Math.Clamp(HorizontalShift, 0, UnitsPerChart - 1);
                    break;
                default:
                {
                    if (KernelShiftValue > 0)
                    {
                        Debug.Assert(HorizontalShift == 0);
                        KernelShift = KernelShiftValue - barsChange;
                    }
                    else
                    {
                        throw new InvalidOperationException("_horizontalShift <= 0 && _kernelShiftValue <= 0");
                    }

                    break;
                }
            }

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
            DebugCanvas!.Invalidate();
#endif
            _previousMouseY = currentMouseY;
            _previousMouseX = currentMouseX;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnPointerMoved");
            throw;
        }
    }

    private void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            IsMouseDown = false;
            GraphCanvas!.ReleasePointerCapture(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnPointerReleased");
            throw;
        }
    }

    private void TextCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "TextCanvas_OnSizeChanged");
            throw;
        }
    }

    private void TextCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(_textBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
        var (upCurrency, downCurrency, upColor, downColor) = IsReversed ? (QuoteCurrency, BaseCurrency, QuoteColor, BaseColor) : (BaseCurrency, QuoteCurrency, BaseColor, QuoteColor);
        using var upCurrencyLayout = new CanvasTextLayout(args.DrawingSession, upCurrency, _currencyFormat, 0.0f, 0.0f);
        using var downCurrencyLayout = new CanvasTextLayout(args.DrawingSession, downCurrency, _currencyFormat, 0.0f, 0.0f);
        var upCurrencyPosition = new Vector2((float)(sender.Size.Width - upCurrencyLayout.DrawBounds.Width - 10), 0);
        var downCurrencyPosition = new Vector2((float)(sender.Size.Width - downCurrencyLayout.DrawBounds.Width - 10), (float)upCurrencyLayout.DrawBounds.Height + 10);
        args.DrawingSession.DrawText(upCurrency, upCurrencyPosition, upColor, _currencyFormat);
        args.DrawingSession.DrawText(downCurrency, downCurrencyPosition, downColor, _currencyFormat);
    }

    private void YAxisCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            var yAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
            YAxisWidth = CalculateTextBounds(YAxisTextSample, YxAxisTextFormat).width;
            var grid = yAxisCanvas.Parent as Grid;
            if (grid == null || grid.ColumnDefinitions.Count <= 1)
            {
                throw new InvalidOperationException("Canvas control has no parent grid.");
            }

            var axisColumn = grid.ColumnDefinitions[1];
            axisColumn.Width = new GridLength(YAxisWidth);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnSizeChanged");
            throw;
        }
    }

    protected abstract void YAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);

    private void YAxisCanvas_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnPointerEntered");
            throw;
        }
    }

    private void YAxisCanvas_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnPointerExited");
            throw;
        }
    }

    private void YAxisCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            IsMouseDown = true;
            _previousMouseY = (float)e.GetCurrentPoint(YAxisCanvas).Position.Y;
            YAxisCanvas!.CapturePointer(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnPointerPressed");
            throw;
        }
    }

    private void YAxisCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (!IsMouseDown)
            {
                return;
            }

            var currentMouseY = (float)e.GetCurrentPoint(YAxisCanvas).Position.Y;
            var deltaY = _previousMouseY - currentMouseY;
            var pipsChange = deltaY / VerticalScale;

            if (Math.Abs(pipsChange) < 1)
            {
                return;
            }

            PipsPerChart += pipsChange;
            PipsPerChart = Math.Clamp(PipsPerChart, MinPipsPerChart, MaxPipsPerChart);
            VerticalScale = GraphHeight / (PipsPerChart - 1);

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
            DebugCanvas!.Invalidate();
#endif
            _previousMouseY = currentMouseY;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnPointerMoved");
            throw;
        }
    }

    private void YAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            IsMouseDown = false;
            YAxisCanvas!.ReleasePointerCapture(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "YAxisCanvas_OnPointerReleased");
            throw;
        }
    }

    private void XAxisCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            var xAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
            XAxisHeight = CalculateTextBounds(XAxisTextSample, YxAxisTextFormat).height;
            var grid = xAxisCanvas.Parent as Grid;
            if (grid == null || grid.RowDefinitions.Count <= 1)
            {
                throw new InvalidOperationException("Canvas control has no parent grid.");
            }

            var axisRow = grid.RowDefinitions[1];
            axisRow.Height = new GridLength(XAxisHeight);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnSizeChanged");
            throw;
        }
    }

    protected abstract void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);

    private void XAxisCanvas_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerEntered");
            throw;
        }
    }

    private void XAxisCanvas_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerExited");
            throw;
        }
    }

    private void XAxisCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            IsMouseDown = true;
            _previousMouseX = (float)e.GetCurrentPoint(XAxisCanvas).Position.X;
            XAxisCanvas!.CapturePointer(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerPressed");
            throw;
        }
    }

    private void XAxisCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (!IsMouseDown)
            {
                return;
            }

            var currentMouseX = (float)e.GetCurrentPoint(XAxisCanvas).Position.X;
            var deltaX = _previousMouseX - currentMouseX;
            var unitsChange = deltaX / HorizontalScale;

            if (Math.Abs(unitsChange) < 1)
            {
                return;
            }

            PendingUnitsPerChart += unitsChange;
            _previousMouseX = currentMouseX;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerMoved");
            throw;
        }
    }

    protected abstract void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e);

    private (float width, float height) CalculateTextBounds(string textSample, CanvasTextFormat textFormat)
    {
        try
        {
            var textLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), textSample, textFormat, float.PositiveInfinity, float.PositiveInfinity);
            var textBounds = textLayout.LayoutBounds;
            return ((float)textBounds.Width, (float)textBounds.Height);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "CalculateTextBounds");
            throw;
        }
    }

    protected DateTime RoundDateTime(DateTime dt, double timeStep)
    {
        try
        {
            dt = dt.AddMilliseconds(-dt.TimeOfDay.Milliseconds);
            var totalSeconds = dt.TimeOfDay.TotalSeconds;
            var modulo = totalSeconds % timeStep;
            return modulo < timeStep / 2 ? dt.AddSeconds(-modulo) : dt.AddSeconds(timeStep - modulo);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "RoundDateTime");
            throw;
        }
    }

    private static (string baseCurrency, string quoteCurrency) GetCurrenciesFromSymbol(Symbol symbol)
    {
        var symbolName = symbol.ToString();
        var first = symbolName[..3];
        var second = symbolName.Substring(3, 3);
        if (Enum.TryParse<Currency>(first, out var baseCurrency) && Enum.TryParse<Currency>(second, out var quoteCurrency))
        {
            return (baseCurrency.ToString(), quoteCurrency.ToString());
        }
        throw new Exception("Failed to parse currencies from symbol.");
    }

    public void Tick()
    {
        if (GraphCanvas == null)
        {
            return;
        }

        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
#if DEBUGWIN2DCanvasControl
        DebugCanvas!.Invalidate();
#endif
    }

    public void Dispose()
    {
       
    }

    #region GraphWidth

    private float _graphWidth;

    protected float GraphWidth
    {
        get => _graphWidth;
        set
        {
            _graphWidth = value;
            AdjustMaxUnitsPerChart();
        }
    }

    protected abstract void AdjustMaxUnitsPerChart();

    #endregion GraphWidth

    #region KernelShift

    protected int KernelShiftValue;

    protected int KernelShift
    {
        get => KernelShiftValue;
        set
        {
            KernelShiftValue = value;
            AdjustKernelShift();
        }
    }

    protected abstract void AdjustKernelShift();

    #endregion KernelShift

#if DEBUGWIN2DCanvasControl
    protected CanvasControl? DebugCanvas;
    protected DebugInfo DebugInfoStruct;

    protected struct DebugInfo
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

    private void DebugCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            var debugCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
            var grid = debugCanvas.Parent as Grid;
            if (grid == null || grid.RowDefinitions.Count <= 1)
            {
                throw new InvalidOperationException("Canvas control has no parent grid.");
            }

            var debugRow = grid.RowDefinitions[2];
            debugRow.Height = new GridLength(CalculateTextBounds(YAxisTextSample, YxAxisTextFormat).height * 3); //todo: 3 rows now
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "DebugCanvas_OnSizeChanged");
            throw;
        }
    }

    protected abstract void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);
#endif
}