/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                ChartControlBaseFirst.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Windows.UI;
using ABI.Windows.UI.Notifications;
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
using Terminal.WinUI3.Models.Notifications;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase
{
    protected CanvasControl?  GraphCanvas;
    private CanvasControl? _centuryAxisCanvas;
    private CanvasControl? _pipsAxisCanvas;
    private CanvasControl? _xAxisCanvas;
    private CanvasControl? _textCanvas;
    private CanvasControl? _debugCanvas;

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
    private const string AxisFontFamily = "Lucida Console";
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
        _centuryAxisCanvas = GetTemplateChild("centuryAxisCanvas") as CanvasControl;
        _pipsAxisCanvas = GetTemplateChild("pipsAxisCanvas") as CanvasControl;
        _xAxisCanvas = GetTemplateChild("xAxisCanvas") as CanvasControl;
        _textCanvas = GetTemplateChild("textCanvas") as CanvasControl;
        _debugCanvas = GetTemplateChild("debugCanvas") as CanvasControl;

        if (GraphCanvas is null || _centuryAxisCanvas is null || _pipsAxisCanvas is null || _xAxisCanvas is null || _textCanvas is null || _debugCanvas is null)
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

        _centuryAxisCanvas.SizeChanged += CenturyAxisCanvasOnSizeChanged;
        _centuryAxisCanvas.Draw += CenturyAxisCanvasOnDraw;
        _centuryAxisCanvas.PointerEntered += CenturyAxisCanvasOnPointerEntered;
        _centuryAxisCanvas.PointerExited += CenturyAxisCanvasOnPointerExited;
        _centuryAxisCanvas.PointerPressed += CenturyAxisCanvasOnPointerPressed;
        _centuryAxisCanvas.PointerMoved += CenturyAxisCanvasOnPointerMoved;
        _centuryAxisCanvas.PointerReleased += CenturyAxisCanvasOnPointerReleased;

        _pipsAxisCanvas.Draw += PipsAxisCanvasOnDraw;
        _pipsAxisCanvas.PointerEntered += PipsAxisCanvasOnPointerEntered;
        _pipsAxisCanvas.PointerExited += PipsAxisCanvasOnPointerExited;
        _pipsAxisCanvas.PointerPressed += PipsAxisCanvasOnPointerPressed;
        _pipsAxisCanvas.PointerMoved += PipsAxisCanvasOnPointerMoved;
        _pipsAxisCanvas.PointerReleased += PipsAxisCanvasOnPointerReleased;

        _xAxisCanvas.SizeChanged += XAxisCanvas_OnSizeChanged;
        _xAxisCanvas.Draw += XAxisCanvas_OnDraw;
        _xAxisCanvas.PointerEntered += XAxisCanvas_OnPointerEntered;
        _xAxisCanvas.PointerExited += XAxisCanvas_OnPointerExited;
        _xAxisCanvas.PointerPressed += XAxisCanvas_OnPointerPressed;
        _xAxisCanvas.PointerMoved += XAxisCanvas_OnPointerMoved;
        _xAxisCanvas.PointerReleased += XAxisCanvas_OnPointerReleased;

        _textCanvas.Draw += TextCanvas_OnDraw;
        _debugCanvas.Draw += DebugCanvas_OnDraw;
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

        if (_centuryAxisCanvas != null)
        {
            _centuryAxisCanvas.SizeChanged -= CenturyAxisCanvasOnSizeChanged;
            _centuryAxisCanvas.Draw -= CenturyAxisCanvasOnDraw;
            _centuryAxisCanvas.PointerEntered -= CenturyAxisCanvasOnPointerEntered;
            _centuryAxisCanvas.PointerExited -= CenturyAxisCanvasOnPointerExited;
            _centuryAxisCanvas.PointerPressed -= CenturyAxisCanvasOnPointerPressed;
            _centuryAxisCanvas.PointerMoved -= CenturyAxisCanvasOnPointerMoved;
            _centuryAxisCanvas.PointerReleased -= CenturyAxisCanvasOnPointerReleased;
        }

        if (_pipsAxisCanvas != null)
        {
            _pipsAxisCanvas.Draw -= PipsAxisCanvasOnDraw;
            _pipsAxisCanvas.PointerEntered -= PipsAxisCanvasOnPointerEntered;
            _pipsAxisCanvas.PointerExited -= PipsAxisCanvasOnPointerExited;
            _pipsAxisCanvas.PointerPressed -= PipsAxisCanvasOnPointerPressed;
            _pipsAxisCanvas.PointerMoved -= PipsAxisCanvasOnPointerMoved;
            _pipsAxisCanvas.PointerReleased -= PipsAxisCanvasOnPointerReleased;
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

        if (_textCanvas != null)
        {
            _textCanvas.Draw -= TextCanvas_OnDraw;
        }

        if (_debugCanvas != null)
        {
            _debugCanvas.Draw -= DebugCanvas_OnDraw;
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

        _pipsPerCentury = Century / TickValue / 10d;
        Pips = (int)(_pipsPerCentury * Centuries);
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
    private void GraphCanvasOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        //throw new NotImplementedException();
    }

    private void GraphCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

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
        //ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
    private void PipsAxisCanvasOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        //ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
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
         //ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
    private void XAxisCanvas_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        //ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
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
        _centuryAxisCanvas!.Invalidate();
        _pipsAxisCanvas!.Invalidate();
        _xAxisCanvas!.Invalidate();
        _textCanvas!.Invalidate();
        _debugCanvas!.Invalidate();
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
    public abstract void RepeatSelectedNotification();
    public abstract void OnRepeatSelectedNotification(NotificationBase notificationBase);
}