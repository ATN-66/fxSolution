/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                ChartControlBase.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using Common.ExtensionsAndHelpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Terminal.WinUI3.Controls;

public abstract partial class ChartControlBase
{
    protected CanvasControl? GraphCanvas;
    private CanvasControl? _textCanvas;
    protected CanvasControl? YAxisCanvas;
    protected CanvasControl? XAxisCanvas;
    protected CanvasControl? DebugCanvas;

    protected bool IsMouseDown;
    protected double PreviousMouseX;
    protected double PreviousMouseY;

    protected double XAxisHeight;
    protected double YAxisWidth;

    private const string YAxisTextSample = "0.12345";
    private const string XAxisTextSample = "HH:mm:ss";
    protected readonly CanvasTextFormat YxAxisTextFormat = new() { FontSize = (float)YAxisFontSize, FontFamily = YAxisFontFamily };
    private readonly CanvasTextFormat _currencyFormat = new() { FontSize = 20, WordWrapping = CanvasWordWrapping.NoWrap };
    private readonly Color _textBackgroundColor = Colors.Transparent;

    protected const double YAxisStepInPips = 10d;
    protected const double YAxisFontSize = 12d;
    private const string YAxisFontFamily = "Lucida Console";

    private const string HexCode = "#202020";
    protected readonly string YxAxisLabelFormat;
    protected readonly Color YAxisAskBidForegroundColor = Colors.White;
    protected readonly Color YxAxisBackgroundColor = Color.FromArgb(255, Convert.ToByte(HexCode.Substring(1, 2), 16), Convert.ToByte(HexCode.Substring(3, 2), 16), Convert.ToByte(HexCode.Substring(5, 2), 16));
    protected readonly Color YxAxisForegroundColor = Colors.Gray;

    protected const int MinTicks = 2;
    protected const int MaxTicks = 20;

    protected readonly Color GraphBackgroundColor = Colors.Black;
    protected readonly Color GraphForegroundColor = Colors.White;
    protected const double GraphDataStrokeThickness = 1d;

    protected const double ArrowheadLength = 10d;
    protected const double ArrowheadWidth = 5d;
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

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        GraphCanvas = GetTemplateChild("graphCanvas") as CanvasControl;
        _textCanvas = GetTemplateChild("textCanvas") as CanvasControl;
        YAxisCanvas = GetTemplateChild("yAxisCanvas") as CanvasControl;
        XAxisCanvas = GetTemplateChild("xAxisCanvas") as CanvasControl;
        DebugCanvas = GetTemplateChild("debugCanvas") as CanvasControl;
        if (GraphCanvas is null || YAxisCanvas is null || XAxisCanvas is null || _textCanvas is null || DebugCanvas is null)
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

        XAxisCanvas.SizeChanged += XAxisCanvas_OnSizeChanged;
        XAxisCanvas.Draw += XAxisCanvas_OnDraw;
        XAxisCanvas.PointerEntered += XAxisCanvas_OnPointerEntered;
        XAxisCanvas.PointerExited += XAxisCanvas_OnPointerExited;
        XAxisCanvas.PointerPressed += XAxisCanvas_OnPointerPressed;
        XAxisCanvas.PointerMoved += XAxisCanvas_OnPointerMoved;
        XAxisCanvas.PointerReleased += XAxisCanvas_OnPointerReleased;

        DebugCanvas.SizeChanged += DebugCanvas_OnSizeChanged;
        DebugCanvas.Draw += DebugCanvas_OnDraw;
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
            PreviousMouseY = (float)e.GetCurrentPoint(GraphCanvas).Position.Y;
            PreviousMouseX = (float)e.GetCurrentPoint(GraphCanvas).Position.X;
            GraphCanvas!.CapturePointer(e.Pointer);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnPointerPressed");
            throw;
        }
    }
    protected abstract void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e);
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

            var axisColumn = grid.ColumnDefinitions[2];
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
            PreviousMouseY = (float)e.GetCurrentPoint(YAxisCanvas).Position.Y;
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
            var deltaY = PreviousMouseY - currentMouseY;
            var pipsChange = deltaY / VerticalScale;

            if (Math.Abs(pipsChange) < 1)
            {
                return;
            }

            Pips += (int)pipsChange;
            Pips = Math.Clamp(PipsPercent, MinPips, MaxPips);
            VerticalScale = GraphHeight / (Pips - 1);

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
            DebugCanvas!.Invalidate();
            PreviousMouseY = currentMouseY;
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

            var axisRow = grid.RowDefinitions[0];
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
            PreviousMouseX = (float)e.GetCurrentPoint(XAxisCanvas).Position.X;
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
            var deltaX = PreviousMouseX - currentMouseX;
            var unitsChange = deltaX / HorizontalScale;

            if (Math.Abs(unitsChange) < 1)
            {
                return;
            }

            PreviousMouseX = currentMouseX;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerMoved");
            throw;
        }
    }
    protected abstract void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e);
}