/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                              ChartControlBase.cs |
  +------------------------------------------------------------------+*/

using System.Runtime.CompilerServices;
using Windows.UI;
using Common.Entities;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Controls.Chart.Candlestick;
using Terminal.WinUI3.Controls.Chart.ThresholdBar;
using Terminal.WinUI3.Controls.Chart.Tick;
using Terminal.WinUI3.Models.Chart;
using Symbol = Common.Entities.Symbol;
using Enum = System.Enum;
using Terminal.WinUI3.Messenger.Chart;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase : Control
{
    protected readonly ILogger<ChartControlBase> Logger;
    private readonly Queue<(int ID, MessageType Type, string Message)> _messageQueue = new();
    private const int DebugMessageQueueSize = 10;
    private int _debugMessageId;
    private readonly MessageType _messageTypeLevel;
    public CommunicationToken? CommunicationToken { get; set; }

    protected ChartControlBase(IConfiguration configuration, ChartSettings chartSettings, double tickValue, Color baseColor, Color quoteColor, ILogger<ChartControlBase> logger)
    {
        Logger = logger;

        Symbol = chartSettings.Symbol;
        IsReversed = chartSettings.IsReversed;
        HorizontalShift = chartSettings.HorizontalShift;
        VerticalShift = chartSettings.VerticalShift;
        SetValue(KernelShiftPercentProperty, chartSettings.KernelShiftPercent);

        var type = GetType();
        ChartType = type switch
        {
            _ when type == typeof(TickChartControl) => ChartType.Ticks,
            _ when type == typeof(CandlestickChartControl) => ChartType.Candlesticks,
            _ when type == typeof(ThresholdBarChartControl) => ChartType.ThresholdBars,
            _ => throw new Exception($"Unknown chart type: {type}")
        };

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
        WeakReferenceMessenger.Default.UnregisterAll(this);
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

    protected void EnqueueMessage(MessageType type, string message, [CallerMemberName] string methodName = "")
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
        _debugCanvas!.Invalidate();
    }
    public void ClearMessages()
    {
        _messageQueue.Clear();
        _debugCanvas!.Invalidate();
    }
}