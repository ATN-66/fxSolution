/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              ChartControlBase.cs |
  +------------------------------------------------------------------+*/
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls;

public abstract partial class ChartControlBase : Control
{
    protected readonly ILogger<ChartControlBase> Logger;
    public readonly bool IsReversed;
    protected readonly double Digits;
    private readonly Queue<(MessageType Type, string Message)> _messageQueue = new();

    protected ChartControlBase(Symbol symbol, bool isReversed, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger)
    {
        Logger = logger;

        Symbol = symbol;
        IsReversed = isReversed;

        switch (Symbol)
        {
            case Symbol.EURGBP:
            case Symbol.EURUSD:
            case Symbol.GBPUSD:
                Digits = 0.0001d;
                YxAxisLabelFormat = "f5";
                break;
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                Digits = 0.01d;
                YxAxisLabelFormat = "f3";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(Symbol), Symbol, null);
        }

        BaseColor = baseColor;
        QuoteColor = quoteColor;
        (BaseCurrency, QuoteCurrency) = GetCurrenciesFromSymbol(Symbol);
    }

    public Symbol  Symbol { get; }
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

    public void Tick()
    {
        if (GraphCanvas == null)
        {
            return;
        }

        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
    }
    public void Dispose()
    {
       
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
    public void EnqueueMessage(MessageType type, string message)
    {
        if (_messageQueue.Count >= 10) 
        {
            _messageQueue.Dequeue();   
        }

        _messageQueue.Enqueue((type, message));
        DebugCanvas!.Invalidate();
    }

    private void DebugCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //try
        //{
        //    var debugCanvas = sender as CanvasControl ?? throw new InvalidOperationException("Canvas controls not found.");
        //    var grid = debugCanvas.Parent as Grid;
        //    if (grid == null || grid.RowDefinitions.Count <= 1)
        //    {
        //        throw new InvalidOperationException("Canvas control has no parent grid.");
        //    }

        //    var debugRow = grid.RowDefinitions[2];
        //    debugRow.Height = new GridLength(CalculateTextBounds(YAxisTextSample, YxAxisTextFormat).height * 3); //todo: 3 rows now
        //}
        //catch (Exception exception)
        //{
        //    LogExceptionHelper.LogException(Logger, exception, "DebugCanvas_OnSizeChanged");
        //    throw;
        //}
    }

    private void DebugCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        //EnqueueMessage(MessageType.Trace, $"width:{GraphWidth:0000}, max units:{MaxUnits:0000}, units:{Units:0000}, units percent:{UnitsPercent:00}");
        //EnqueueMessage(MessageType.Trace, $"height:{GraphHeight:0000}, max pips:{MaxPips:0000}, pips:{Pips:0000}, pips percent:{PipsPercent:00}");
        //EnqueueMessage(MessageType.Trace, $"horizontal shift:{HorizontalShift:0000}, kernel shift percent:{KernelShiftPercent:00}, kernel shift:{KernelShift:000000}, kernel.Count:{Kernel.Count:000000}");

        args.DrawingSession.Clear(_textBackgroundColor);
        args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        float y = 0;

        foreach (var (type, message) in _messageQueue)
        {
            if (type >= MessageType.Trace) 
            {
                var textLayout = new CanvasTextLayout(args.DrawingSession, message, YxAxisTextFormat, float.PositiveInfinity, float.PositiveInfinity);
                args.DrawingSession.DrawTextLayout(textLayout, 0, y, Colors.White);
                y += (float)textLayout.LayoutBounds.Height;
            }
        }
    }

    public void ResetShifts()
    {
        VerticalShift = 0;
        KernelShift = 0;
        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
    }
}