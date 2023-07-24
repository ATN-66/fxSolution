/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Services |
  |                                                 VisualService.cs |
  +------------------------------------------------------------------+*/

using Windows.UI;
using Common.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Services;

public class VisualService : IVisualService
{
    private readonly IDispatcherService _dispatcherService;

    private Dictionary<Symbol, Dictionary<ChartType, IKernel>> _kernels = null!;

    private readonly Dictionary<Symbol, TickChartControl?> _tickChartsReversed = new();
    private readonly Dictionary<Symbol, TickChartControl?> _tickCharts = new();
    private readonly Dictionary<Symbol, CandlestickChartControl?> _candlestickChartsReversed = new();
    private readonly Dictionary<Symbol, CandlestickChartControl?> _candlestickCharts = new();

    private readonly Dictionary<Symbol, CurrencyColors> _symbolColorsMap = new()
    {
        { Symbol.EURUSD, new CurrencyColors(Colors.SkyBlue, Colors.LimeGreen) },
        { Symbol.GBPUSD, new CurrencyColors(Colors.MediumPurple, Colors.LimeGreen) },
        { Symbol.USDJPY, new CurrencyColors(Colors.LimeGreen, Colors.Goldenrod) },
        { Symbol.EURGBP, new CurrencyColors(Colors.SkyBlue, Colors.MediumPurple) },
        { Symbol.EURJPY, new CurrencyColors(Colors.SkyBlue, Colors.Goldenrod) },
        { Symbol.GBPJPY, new CurrencyColors(Colors.MediumPurple, Colors.Goldenrod) }
    };

    public VisualService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    public void Initialize(Dictionary<Symbol, Dictionary<ChartType, IKernel>> kernels)
    {
        _kernels = kernels;

        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _tickChartsReversed[(Symbol)symbol] = null;
            _tickCharts[(Symbol)symbol] = null;
            _candlestickChartsReversed[(Symbol)symbol] = null;
            _candlestickCharts[(Symbol)symbol] = null;
        }
    }

    public T GetChart<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : ChartControl<TItem, TK> where TItem : IChartItem where TK : IKernel<TItem>
    {
        switch (chartType)
        {
            case ChartType.Ticks:
                var quotationKernel = _kernels[symbol][ChartType.Ticks] as QuotationKernel ?? throw new InvalidCastException();
                if (isReversed)
                {
                    _tickChartsReversed[symbol] = new TickChartControl(symbol, true, quotationKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    var result = _tickChartsReversed[symbol] as T;
                    return result!;
                }
                else
                {
                    _tickCharts[symbol] = new TickChartControl(symbol, false, quotationKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    var result = _tickCharts[symbol] as T;
                    return result!;
                }
            case ChartType.Candlesticks:
                var candlestickKernel = _kernels[symbol][ChartType.Candlesticks] as CandlestickKernel ?? throw new InvalidCastException();
                if (isReversed)
                {
                    _candlestickChartsReversed[symbol] = new CandlestickChartControl(symbol, true, candlestickKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    var result = _candlestickChartsReversed[symbol] as T;
                    return result!;
                }
                else
                {
                    _candlestickCharts[symbol] = new CandlestickChartControl(symbol, false, candlestickKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    var result = _candlestickCharts[symbol] as T;
                    return result!;
                }
            default: throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType));
        }
    }

    public void DisposeChart<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed)
    {
        switch (chartType)
        {
            case ChartType.Ticks:
                if (isReversed)
                {
                    _tickChartsReversed[symbol]?.Dispose();
                    _tickChartsReversed[symbol] = null;
                }
                else
                {
                    _tickCharts[symbol]?.Dispose();
                    _tickCharts[symbol] = null;
                }
                break;
            case ChartType.Candlesticks:
                if (isReversed)
                {
                    _candlestickChartsReversed[symbol]?.Dispose();
                    _candlestickChartsReversed[symbol] = null;
                     
                }
                else
                {
                    _candlestickCharts[symbol]?.Dispose();
                    _candlestickCharts[symbol] = null;
                }
                break;
            default: throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType));
        }
    }

    public void Tick(Symbol symbol)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            _tickChartsReversed[symbol]?.Tick();
            _tickCharts[symbol]?.Tick();
            _candlestickChartsReversed[symbol]?.Tick();
            _candlestickCharts[symbol]?.Tick();
        });
    }
}

public class CurrencyColors
{
    public Color BaseColor
    {
        get;
    }
    public Color QuoteColor
    {
        get;
    }

    public CurrencyColors(Color baseColor, Color quoteColor)
    {
        BaseColor = baseColor;
        QuoteColor = quoteColor;
    }
}