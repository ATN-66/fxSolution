/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                ChartControlBaseFirst.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using Common.ExtensionsAndHelpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase
{
    protected CanvasControl?  GraphCanvas;
    protected CanvasControl? CenturyAxisCanvas;
    protected CanvasControl? PipsAxisCanvas;
    protected CanvasControl? XAxisCanvas;
    private CanvasControl? _textCanvas;
    protected CanvasControl? DebugCanvas;

    protected bool IsMouseDown;
    protected double PreviousMouseX;
    protected double PreviousMouseY;

    private double _centuryAxisWidth;
    protected double PipsAxisWidth;
    private double _xAxisHeight;

    protected readonly CanvasStrokeStyle NotificationStrokeStyleSelected = new() { DashStyle = CanvasDashStyle.DashDot };
    protected readonly CanvasStrokeStyle NotificationStrokeStyle = new() { DashStyle = CanvasDashStyle.Solid };
    protected readonly CanvasTextFormat VerticalTextFormat = new() { FontSize = 10f, FontFamily = AxisFontFamily, WordWrapping = CanvasWordWrapping.NoWrap, Direction = CanvasTextDirection.TopToBottomThenRightToLeft };
    protected readonly CanvasTextFormat HorizontalTextFormat = new () { FontSize = 10f, FontFamily = AxisFontFamily, WordWrapping = CanvasWordWrapping.NoWrap, Direction = CanvasTextDirection.LeftToRightThenTopToBottom};

    private const string CenturyAxisTextSample = "-1234";
    private const string PipsAxisTextSample = "1.234";
    private const string XAxisTextSample = "HH:mm:ss";

    protected readonly CanvasTextFormat YAxisCanvasTextFormat = new() { FontSize = AxisFontSize, FontFamily = AxisFontFamily, WordWrapping = CanvasWordWrapping.NoWrap }; 
    protected readonly CanvasTextFormat CurrencyLabelCanvasTextFormat = new() { FontSize = CurrencyFontSize, FontFamily = CurrencyFontFamily, WordWrapping = CanvasWordWrapping.NoWrap };
    protected readonly CanvasTextFormat AskBidLabelCanvasTextFormat = new() { FontSize = AskBidFontSize, FontFamily = CurrencyFontFamily, WordWrapping = CanvasWordWrapping.NoWrap };

    protected const float AxisFontSize = 12f;
    private const float AskBidFontSize = 18f;
    protected const float CurrencyFontSize = AskBidFontSize * 2;
    protected const string AxisFontFamily = "Lucida Console";
    private const string CurrencyFontFamily = "Lucida Console";

    protected readonly string PriceTextFormat;
    protected readonly string PriceLabelTextFormat;
    protected readonly Color AxisBackgroundColor = Colors.Black;
    protected readonly Color AxisForegroundColor = Colors.Gray;

    protected readonly Color GraphBackgroundColor = Colors.Black;
    protected readonly Color GraphForegroundColor = Colors.White;
    protected const double GraphDataStrokeThickness = 1d;

    protected const double YAxisStepInPips = 10d;

    protected const double ArrowheadLength = 10d;
    protected const double ArrowheadWidth = 5d;
    protected readonly IList<(Vector2 startPoint, Vector2 endPoint)> ArrowLines = new List<(Vector2, Vector2)>
    {
        new(new Vector2(10, 10), new Vector2(110, 10)),
        //new(new Vector2(110, 10), new Vector2(110, 110)),
        //new(new Vector2(110, 110), new Vector2(10, 110)),
        //new(new Vector2(10, 110), new Vector2(10, 10)),
        //new(new Vector2(10, 10), new Vector2(60, 60)),
        //new(new Vector2(110, 10), new Vector2(60, 60)),
        //new( new Vector2(110, 110), new Vector2(60, 60)),
        //new(new Vector2(10, 110), new Vector2(60, 60))
    };

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        GraphCanvas = GetTemplateChild("graphCanvas") as CanvasControl;
        CenturyAxisCanvas = GetTemplateChild("centuryAxisCanvas") as CanvasControl;
        PipsAxisCanvas = GetTemplateChild("pipsAxisCanvas") as CanvasControl;
        XAxisCanvas = GetTemplateChild("xAxisCanvas") as CanvasControl;
        _textCanvas = GetTemplateChild("textCanvas") as CanvasControl;
        DebugCanvas = GetTemplateChild("debugCanvas") as CanvasControl;

        if (GraphCanvas is null || CenturyAxisCanvas is null || PipsAxisCanvas is null || XAxisCanvas is null || _textCanvas is null || DebugCanvas is null)
        {
            throw new InvalidOperationException("Canvas controls not found in the template.");
        }

        GraphCanvas.SizeChanged += GraphCanvas_OnSizeChanged;
        GraphCanvas.Draw += GraphCanvas_OnDraw;
        GraphCanvas.PointerPressed += GraphCanvas_OnPointerPressed;
        GraphCanvas.PointerMoved += GraphCanvas_OnPointerMoved;
        GraphCanvas.PointerReleased += GraphCanvas_OnPointerReleased;
        GraphCanvas.DoubleTapped += GraphCanvas_OnDoubleTapped;
        GraphCanvas.RightTapped += GraphCanvas_RightTapped;
        GraphCanvas.PointerWheelChanged += GraphCanvasOnPointerWheelChanged;

        CenturyAxisCanvas.SizeChanged += CenturyAxisCanvasOnSizeChanged;
        CenturyAxisCanvas.Draw += CenturyAxisCanvasOnDraw;
        CenturyAxisCanvas.PointerEntered += CenturyAxisCanvasOnPointerEntered;
        CenturyAxisCanvas.PointerExited += CenturyAxisCanvasOnPointerExited;
        CenturyAxisCanvas.PointerPressed += CenturyAxisCanvasOnPointerPressed;
        CenturyAxisCanvas.PointerMoved += CenturyAxisCanvasOnPointerMoved;
        CenturyAxisCanvas.PointerReleased += CenturyAxisCanvasOnPointerReleased;

        PipsAxisCanvas.Draw += PipsAxisCanvasOnDraw;
        PipsAxisCanvas.PointerEntered += PipsAxisCanvasOnPointerEntered;
        PipsAxisCanvas.PointerExited += PipsAxisCanvasOnPointerExited;
        PipsAxisCanvas.PointerPressed += PipsAxisCanvasOnPointerPressed;
        PipsAxisCanvas.PointerMoved += PipsAxisCanvasOnPointerMoved;
        PipsAxisCanvas.PointerReleased += PipsAxisCanvasOnPointerReleased;

        XAxisCanvas.SizeChanged += XAxisCanvas_OnSizeChanged;
        XAxisCanvas.Draw += XAxisCanvas_OnDraw;
        XAxisCanvas.PointerEntered += XAxisCanvas_OnPointerEntered;
        XAxisCanvas.PointerExited += XAxisCanvas_OnPointerExited;
        XAxisCanvas.PointerPressed += XAxisCanvas_OnPointerPressed;
        XAxisCanvas.PointerMoved += XAxisCanvas_OnPointerMoved;
        XAxisCanvas.PointerReleased += XAxisCanvas_OnPointerReleased;

        _textCanvas.Draw += TextCanvas_OnDraw;
        DebugCanvas.Draw += DebugCanvas_OnDraw;
    }

    private void GraphCanvasOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void GraphCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        
    }

    public void Detach()
    {
        if (GraphCanvas != null)
        {
            GraphCanvas.SizeChanged -= GraphCanvas_OnSizeChanged;
            GraphCanvas.Draw -= GraphCanvas_OnDraw;
            GraphCanvas.PointerPressed -= GraphCanvas_OnPointerPressed;
            GraphCanvas.PointerMoved -= GraphCanvas_OnPointerMoved;
            GraphCanvas.PointerReleased -= GraphCanvas_OnPointerReleased;
            GraphCanvas.DoubleTapped -= GraphCanvas_OnDoubleTapped;
            GraphCanvas.RightTapped -= GraphCanvas_RightTapped;
            GraphCanvas.PointerWheelChanged -= GraphCanvasOnPointerWheelChanged;
        }

        if (CenturyAxisCanvas != null)
        {
            CenturyAxisCanvas.SizeChanged -= CenturyAxisCanvasOnSizeChanged;
            CenturyAxisCanvas.Draw -= CenturyAxisCanvasOnDraw;
            CenturyAxisCanvas.PointerEntered -= CenturyAxisCanvasOnPointerEntered;
            CenturyAxisCanvas.PointerExited -= CenturyAxisCanvasOnPointerExited;
            CenturyAxisCanvas.PointerPressed -= CenturyAxisCanvasOnPointerPressed;
            CenturyAxisCanvas.PointerMoved -= CenturyAxisCanvasOnPointerMoved;
            CenturyAxisCanvas.PointerReleased -= CenturyAxisCanvasOnPointerReleased;
        }

        if (PipsAxisCanvas != null)
        {
            PipsAxisCanvas.Draw -= PipsAxisCanvasOnDraw;
            PipsAxisCanvas.PointerEntered -= PipsAxisCanvasOnPointerEntered;
            PipsAxisCanvas.PointerExited -= PipsAxisCanvasOnPointerExited;
            PipsAxisCanvas.PointerPressed -= PipsAxisCanvasOnPointerPressed;
            PipsAxisCanvas.PointerMoved -= PipsAxisCanvasOnPointerMoved;
            PipsAxisCanvas.PointerReleased -= PipsAxisCanvasOnPointerReleased;
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

        if (_textCanvas != null)
        {
            _textCanvas.Draw -= TextCanvas_OnDraw;
        }

        if (DebugCanvas != null)
        {
            DebugCanvas.Draw -= DebugCanvas_OnDraw;
        }
    }

    protected virtual void GraphCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _xAxisHeight = CalculateTextBounds(XAxisTextSample, YAxisCanvasTextFormat).height;
        var xAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        var grid = xAxisCanvas.Parent as Grid;
        if (grid == null || grid.RowDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }
        var axisRow = grid.RowDefinitions[1];
        axisRow.Height = new GridLength(_xAxisHeight);

        _centuryAxisWidth = CalculateTextBounds(CenturyAxisTextSample, YAxisCanvasTextFormat).width;
        var centuryAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        var centuryAxisGrid = centuryAxisCanvas.Parent as Grid;
        if (centuryAxisGrid == null || centuryAxisGrid.ColumnDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }
        var centuryAxisColumn = centuryAxisGrid.ColumnDefinitions[0];
        centuryAxisColumn.Width = new GridLength(_centuryAxisWidth);

        PipsAxisWidth = CalculateTextBounds(PipsAxisTextSample, YAxisCanvasTextFormat).width;
        var pipsAxisCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        var pipsAxisGrid = pipsAxisCanvas.Parent as Grid;
        if (pipsAxisGrid == null || pipsAxisGrid.ColumnDefinitions.Count <= 1)
        {
            throw new InvalidOperationException("Canvas control has no parent grid.");
        }
        var pipsAxisColumn = pipsAxisGrid.ColumnDefinitions[1];
        pipsAxisColumn.Width = new GridLength(PipsAxisWidth);

        GraphHeight = e.NewSize.Height;
        Centuries = MinCenturies + (MaxCenturies - MinCenturies) * CenturiesPercent / 100d;

        PipsPerCentury = Century / TickValue / 10d;
        Pips = (int)(PipsPerCentury * Centuries);
        VerticalScale = GraphHeight / Pips;

        GraphWidth = e.NewSize.Width;
        MaxUnits = CalculateMaxUnits();
        Units = Math.Max(MinUnits, MaxUnits * UnitsPercent / 100);
        HorizontalScale = GraphWidth / (Units - 1);
    }
    protected abstract void GraphCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);
    protected virtual void GraphCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        IsMouseDown = true;
        PreviousMouseY = (float)e.GetCurrentPoint(GraphCanvas).Position.Y;
        PreviousMouseX = (float)e.GetCurrentPoint(GraphCanvas).Position.X;
        GraphCanvas!.CapturePointer(e.Pointer);
    }
    protected abstract void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e);
    protected virtual void GraphCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        IsMouseDown = false;
        GraphCanvas!.ReleasePointerCapture(e.Pointer);
    }
    protected abstract void GraphCanvas_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e);

    protected abstract void CenturyAxisCanvasOnSizeChanged(object sender, SizeChangedEventArgs e);
    protected abstract void CenturyAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args);
    private void CenturyAxisCanvasOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
    }
    private void CenturyAxisCanvasOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
    }
    private void CenturyAxisCanvasOnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
    }
    private void CenturyAxisCanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
    }
    private void CenturyAxisCanvasOnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
    }

    protected abstract void PipsAxisCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args);
    private void PipsAxisCanvasOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "PipsAxisCanvasOnPointerEntered");
            throw;
        }
    }
    private void PipsAxisCanvasOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "PipsAxisCanvasOnPointerExited");
            throw;
        }
    }
    private void PipsAxisCanvasOnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //IsMouseDown = true;
        //PreviousMouseY = (float)e.GetCurrentPoint(PipsAxisCanvas).Position.Y;
        //PipsAxisCanvas!.CapturePointer(e.Pointer);
    }
    private void PipsAxisCanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            //if (!IsMouseDown)
            //{
            //    return;
            //}

            //var currentMouseY = (float)e.GetCurrentPoint(PipsAxisCanvas).Position.Y;
            //var deltaY = PreviousMouseY - currentMouseY;
            //var pipsChange = deltaY / VerticalScale;

            //if (Math.Abs(pipsChange) < 1)
            //{
            //    return;
            //}

            //Pips += (int)pipsChange;
            //Pips = Math.Clamp(PipsPercent, MinPips, MaxPips);
            //VerticalScale = GraphHeight / (Pips - 1);

            //Invalidate();
            //PreviousMouseY = currentMouseY;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "PipsAxisCanvasOnPointerMoved");
            throw;
        }
    }
    private void PipsAxisCanvasOnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        //IsMouseDown = false;
        //PipsAxisCanvas!.ReleasePointerCapture(e.Pointer);
    }

    private void XAxisCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
    }
    protected abstract void XAxisCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);
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
        //try
        //{
        //    IsMouseDown = true;
        //    PreviousMouseX = (float)e.GetCurrentPoint(XAxisCanvas).Position.X;
        //    XAxisCanvas!.CapturePointer(e.Pointer);
        //}
        //catch (Exception exception)
        //{
        //    LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerPressed");
        //    throw;
        //}
    }
    private void XAxisCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        //try
        //{
        //    if (!IsMouseDown)
        //    {
        //        return;
        //    }

        //    var currentMouseX = (float)e.GetCurrentPoint(XAxisCanvas).Position.X;
        //    var deltaX = PreviousMouseX - currentMouseX;
        //    var unitsChange = deltaX / HorizontalScale;

        //    if (Math.Abs(unitsChange) < 1)
        //    {
        //        return;
        //    }

        //    PreviousMouseX = currentMouseX;
        //}
        //catch (Exception exception)
        //{
        //    LogExceptionHelper.LogException(Logger, exception, "XAxisCanvas_OnPointerMoved");
        //    throw;
        //}
    }
    protected abstract void XAxisCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e);

    protected abstract void TextCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args);
    private void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Colors.Transparent);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        var x = (float)sender.ActualWidth / 5;
        var y = 0f; // Start from the top.

        foreach (var (id, type, message) in _messageQueue)
        {
            if (type < _messageTypeLevel)
            {
                continue;
            }

            using var textLayout = new CanvasTextLayout(args.DrawingSession, $"{id:##00}: {message}", YAxisCanvasTextFormat, float.PositiveInfinity, float.PositiveInfinity);
            args.DrawingSession.DrawTextLayout(textLayout, x, y, Colors.White);
            y += (float)textLayout.LayoutBounds.Height;
        }
    }

    public void Invalidate()
    {
        if (GraphCanvas is null)
        {
            return;
        }

        GraphCanvas!.Invalidate();
        CenturyAxisCanvas!.Invalidate();
        PipsAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        _textCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
    }

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
    protected abstract int CalculateMaxUnits();
    public abstract void DeleteSelectedNotification();
    public abstract void DeleteAllNotifications();
}