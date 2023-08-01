/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              ChartControlBaseFirst.cs |
  +------------------------------------------------------------------+*/

using System.Runtime.CompilerServices;
using Windows.UI;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Models.Chart;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase : Control
{
    protected readonly ILogger<ChartControlBase> Logger;
    public readonly bool IsReversed;
    protected readonly double Digits;
    protected int DecimalPlaces
    {
        get
        {
            var value = Digits;
            var count = 0;
            while (value < 1)
            {
                value *= 10;
                count++;
            }
            return count;
        }
    }
    protected const double Century = 100d; // todo: settings // one hundred dollars of the balance currency
  
    private readonly Queue<(int ID, MessageType Type, string Message)> _messageQueue = new();
    protected const int DebugMessageQueueSize = 10;
    private int _debugMessageId;
    private readonly MessageType _messageTypeLevel;

    protected ChartControlBase(IConfiguration configuration, Symbol symbol, bool isReversed, double tickValue, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger)
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
                PriceTextFormat = "f5";
                PriceLabelTextFormat = "f3";
                break;
            case Symbol.USDJPY:
            case Symbol.EURJPY:
            case Symbol.GBPJPY:
                Digits = 0.01d;
                PriceTextFormat = "f3";
                PriceLabelTextFormat = "f1";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(Symbol), Symbol, null);
        }

        BaseColor = baseColor;
        QuoteColor = quoteColor;
        (BaseCurrency, QuoteCurrency) = GetCurrenciesFromSymbol(Symbol);

        TickValue = tickValue;

        var messageTypeStr = configuration.GetValue<string>($"{nameof(_messageTypeLevel)}")!;
        _messageTypeLevel = Enum.TryParse(messageTypeStr, out MessageType parsedMessageType) ? parsedMessageType : MessageType.Trace;
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
    protected double TickValue
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

        Invalidate();
    }
    public void Dispose()
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);
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
    public void EnqueueMessage(MessageType type, string message, [CallerMemberName] string methodName = "")
    {
        var latestSameMethodMessage = _messageQueue
            .Reverse()
            .FirstOrDefault(t => t.Message.StartsWith($"{methodName}:"));

        if (latestSameMethodMessage.Message == $"{methodName}: {message}")
        {
            return;
        }

        if (_messageQueue.Count > DebugMessageQueueSize)
        {
            _messageQueue.Dequeue();
        }

        _messageQueue.Enqueue((++_debugMessageId, type, $"{methodName}: {message}"));
        DebugCanvas!.Invalidate();
    }
    public void ClearMessages()
    {
        _messageQueue.Clear();
        DebugCanvas!.Invalidate();
    }
    public void ResetShifts()
    {
        VerticalShift = 0;
        KernelShift = 0;
        Invalidate();
    }
}