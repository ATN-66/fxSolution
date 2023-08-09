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
using Enum = System.Enum;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase : Control
{
    protected readonly ILogger<ChartControlBase> Logger;
    protected readonly bool IsReversed;
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
    private const double Century = 100d; // todo: settings // one hundred dollars of the balance currency
    private readonly Queue<(int ID, MessageType Type, string Message)> _messageQueue = new();
    private const int DebugMessageQueueSize = 10;
    private int _debugMessageId;
    private readonly MessageType _messageTypeLevel;
    protected readonly ViewPort ViewPort = new();

    protected ChartControlBase(IConfiguration configuration, ChartSettings chartSettings, double tickValue, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger)
    {
        Logger = logger;

        Symbol = chartSettings.Symbol;
        IsReversed = chartSettings.IsReversed;
        HorizontalShift = chartSettings.HorizontalShift;
        VerticalShift = chartSettings.VerticalShift;
        SetValue(KernelShiftPercentProperty, chartSettings.KernelShiftPercent);

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

    protected Symbol  Symbol { get; }
    public Currency BaseCurrency
    {
        get;
        set;
    }
    public Currency QuoteCurrency
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

    private static (Currency baseCurrency, Currency quoteCurrency) GetCurrenciesFromSymbol(Symbol symbol)
    {
        var symbolName = symbol.ToString();
        var first = symbolName[..3];
        var second = symbolName[3..];
        if (Enum.TryParse<Currency>(first, out var baseCurrency) && Enum.TryParse<Currency>(second, out var quoteCurrency))
        {
            return (baseCurrency, quoteCurrency);
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
    protected double GetPrice(float positionY)
    {
        if (IsReversed)
        {
            return ViewPort.High - (ViewPort.High - ViewPort.Low) * (positionY / GraphHeight);
        }
        else
        {
            return ViewPort.Low + (ViewPort.High - ViewPort.Low) * (1 - positionY / GraphHeight);
        }
    }
    protected float GetPositionY(double price)
    {
        if (IsReversed)
        {
            return (float)(((price - ViewPort.Low) / (ViewPort.High - ViewPort.Low)) * GraphHeight);
        }
        else
        {
            return (float)((1 - ((price - ViewPort.Low) / (ViewPort.High - ViewPort.Low))) * GraphHeight);
        }
    }
}