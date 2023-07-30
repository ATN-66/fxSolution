/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Services |
  |                                                 ChartService.cs |
  +------------------------------------------------------------------+*/

using Windows.UI;
using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Services;

public class ChartService : IChartService
{
    private readonly IConfiguration _configuration;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IDispatcherService _dispatcherService;

    private Dictionary<Symbol, Dictionary<bool, ChartType>> _settings = null!;
    private Dictionary<Symbol, Dictionary<ChartType, IKernel>> _kernels = null!;

    private readonly Dictionary<Symbol, TickChartControl?> _tickChartsReversed = new();
    private readonly Dictionary<Symbol, TickChartControl?> _tickCharts = new();
    private readonly Dictionary<Symbol, CandlestickChartControl?> _candlestickChartsReversed = new();
    private readonly Dictionary<Symbol, CandlestickChartControl?> _candlestickCharts = new();
    private readonly Dictionary<Symbol, ThresholdBarChartControl?> _thresholdBarChartsReversed = new();
    private readonly Dictionary<Symbol, ThresholdBarChartControl?> _thresholdBarCharts = new();

    private readonly Dictionary<Tuple<Symbol, bool>, int> _tickChartsCounter = new();
    private readonly Dictionary<Tuple<Symbol, bool>, int> _candlestickChartsCounter = new();
    private readonly Dictionary<Tuple<Symbol, bool>, int> _thresholdBarChartsCounter = new();

    private readonly Dictionary<Symbol, double> _tickValues = new();
    private readonly Dictionary<Symbol, CurrencyColors> _symbolColorsMap = new()
    {
        { Symbol.EURUSD, new CurrencyColors(Color.FromArgb(255, 108, 181, 255), Colors.LimeGreen) },
        { Symbol.GBPUSD, new CurrencyColors(Colors.MediumPurple, Colors.LimeGreen) },
        { Symbol.USDJPY, new CurrencyColors(Colors.LimeGreen, Colors.Goldenrod) },
        { Symbol.EURGBP, new CurrencyColors(Color.FromArgb(255, 108, 181, 255), Colors.MediumPurple) },
        { Symbol.EURJPY, new CurrencyColors(Color.FromArgb(255, 108, 181, 255), Colors.Goldenrod) },
        { Symbol.GBPJPY, new CurrencyColors(Colors.MediumPurple, Colors.Goldenrod) }
    };

    [Obsolete("Obsolete")]
    public ChartService(IConfiguration configuration, ILocalSettingsService localSettingsService, IDispatcherService dispatcherService)
    {
        _configuration = configuration;
        _localSettingsService = localSettingsService;
        _dispatcherService = dispatcherService;
        App.MainWindow.GetAppWindow().Closing += OnClosing;
    }

    public Task InitializeAsync(Dictionary<Symbol, Dictionary<ChartType, IKernel>> kernels)
    {
        _kernels = kernels;

        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _tickChartsReversed[(Symbol)symbol] = null;
            _tickCharts[(Symbol)symbol] = null;
            _candlestickChartsReversed[(Symbol)symbol] = null;
            _candlestickCharts[(Symbol)symbol] = null;
            _thresholdBarChartsReversed[(Symbol)symbol] = null;
            _thresholdBarCharts[(Symbol)symbol] = null;
        }

        return LoadSettingsAsync();
    }

    public async Task<ChartControlBase> GetDefaultChartAsync(Symbol symbol, bool isReversed)
    {
        ChartType chartType;

        if (_settings.ContainsKey(symbol) && _settings[symbol].ContainsKey(isReversed))
        {
            chartType = _settings[symbol][isReversed];
        }
        else
        {
            chartType = ChartType.Candlesticks;
        }

        return chartType switch
        {
            ChartType.Ticks => await GetChartAsync<TickChartControl, Quotation, QuotationKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            ChartType.Candlesticks => await GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            ChartType.ThresholdBar => await GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            _ => throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType))
        };
    }

    public async Task<ChartControlBase> GetChartByTypeAsync(Symbol symbol, bool isReversed, ChartType chartType)
    {
        return chartType switch
        {
            ChartType.Ticks => await GetChartAsync<TickChartControl, Quotation, QuotationKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            ChartType.Candlesticks => await GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            ChartType.ThresholdBar => await GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBarKernel>(symbol, chartType, isReversed).ConfigureAwait(false),
            _ => throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType))
        };
    }

    //public T GetChart<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : ChartControl<TItem, TK> where TItem : IChartItem where TK : IKernel<TItem>
    public async Task<T> GetChartAsync<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : ChartControl<TItem, TK> where TItem : IChartItem where TK : IKernel<TItem>
    {
        T result;
        switch (chartType)
        {
            case ChartType.Ticks:
                var quotationKernel = _kernels[symbol][ChartType.Ticks] as QuotationKernel ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_tickChartsReversed[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _tickChartsReversed[symbol] = null;
                    }
                    _tickChartsReversed[symbol] = new TickChartControl(_configuration, symbol, true, _tickValues[symbol], quotationKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_tickChartsReversed[symbol] as T)!;
                    RegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_tickCharts[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _tickCharts[symbol] = null;
                    }
                    _tickCharts[symbol] = new TickChartControl(_configuration, symbol, false, _tickValues[symbol], quotationKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_tickCharts[symbol] as T)!;
                    RegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.Candlesticks:
                var candlestickKernel = _kernels[symbol][ChartType.Candlesticks] as CandlestickKernel ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_candlestickChartsReversed[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _candlestickChartsReversed[symbol] = null;
                    }
                    _candlestickChartsReversed[symbol] = new CandlestickChartControl(_configuration, symbol, true, _tickValues[symbol], candlestickKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_candlestickChartsReversed[symbol] as T)!;
                    RegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_candlestickCharts[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _candlestickCharts[symbol] = null;
                    }
                    _candlestickCharts[symbol] = new CandlestickChartControl(_configuration, symbol, false, _tickValues[symbol], candlestickKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_candlestickCharts[symbol] as T)!;
                    RegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.ThresholdBar:
                var thresholdBarKernel = _kernels[symbol][ChartType.ThresholdBar] as ThresholdBarKernel ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_thresholdBarChartsReversed[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _thresholdBarChartsReversed[symbol] = null;
                    }
                    _thresholdBarChartsReversed[symbol] = new ThresholdBarChartControl(_configuration, symbol, true, _tickValues[symbol], thresholdBarKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_thresholdBarChartsReversed[symbol] as T)!;
                    RegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_thresholdBarCharts[symbol] != null)
                    {
                        DisposeChart(symbol, chartType, isReversed);
                        _thresholdBarCharts[symbol] = null;
                    }
                    _thresholdBarCharts[symbol] = new ThresholdBarChartControl(_configuration, symbol, false, _tickValues[symbol], thresholdBarKernel, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_thresholdBarCharts[symbol] as T)!;
                    RegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                break;
            default: throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType));
        }
        await result.InitializeAsync().ConfigureAwait(false);
        return result;
    }

    public void DisposeChart(ChartControlBase chartControlBase)
    {
        var symbol = chartControlBase.Symbol;
        var isReversed = chartControlBase.IsReversed;
        var name = chartControlBase.GetType().Name;
        ChartType chartType;
        switch (name)
        {
            case nameof(TickChartControl):
                chartType = ChartType.Ticks;
                if (_tickChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
                    DisposeChart(symbol, chartType, isReversed);
                }
                UnRegisterChart(_tickChartsCounter, symbol, isReversed);
                break;
            case nameof(CandlestickChartControl):
                chartType = ChartType.Candlesticks;
                if (_candlestickChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
                    DisposeChart(symbol, chartType, isReversed);
                }
                UnRegisterChart(_candlestickChartsCounter, symbol, isReversed);
                break;
            case nameof(ThresholdBarChartControl):
                chartType = ChartType.ThresholdBar;
                if (_thresholdBarChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
                    DisposeChart(symbol, chartType, isReversed);
                }
                UnRegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                break;
            default:
                throw new Exception($@"Unsupported chart type {name}");
        }
    }

    private void DisposeChart(Symbol symbol, ChartType chartType, bool isReversed)
    {
        _settings[symbol][isReversed] = chartType;

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
            case ChartType.ThresholdBar:
                if (isReversed)
                {
                    _thresholdBarChartsReversed[symbol]?.Dispose();
                    _thresholdBarChartsReversed[symbol] = null;
                }
                else
                {
                    _thresholdBarCharts[symbol]?.Dispose();
                    _thresholdBarCharts[symbol] = null;
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
            _thresholdBarChartsReversed[symbol]?.Tick();
            _thresholdBarCharts[symbol]?.Tick();
        });
    }

    private void OnClosing(AppWindow appWindow, AppWindowClosingEventArgs appWindowClosingEventArgs)
    {
        SaveSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var serializableSettings = await _localSettingsService.ReadSettingAsync<Dictionary<string, Dictionary<string, string>>>("chartSettings").ConfigureAwait(false);

        if (serializableSettings != null)
        {
            _settings = serializableSettings.ToDictionary(kvp => Enum.Parse<Symbol>(kvp.Key), kvp => kvp.Value.ToDictionary(innerKvp => bool.Parse(innerKvp.Key), innerKvp => Enum.Parse<ChartType>(innerKvp.Value)));
        }
        else
        {
            _settings = new Dictionary<Symbol, Dictionary<bool, ChartType>>();
            foreach (var symbol in Enum.GetValues(typeof(Symbol)))
            {
                _settings[(Symbol)symbol] = new Dictionary<bool, ChartType>
                {
                    { false, ChartType.Candlesticks },
                    { true, ChartType.Candlesticks }
                };  
            }
        }
    }

    private void SaveSettingsAsync()
    {
        var serializableSettings = _settings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToDictionary(innerKvp => innerKvp.Key.ToString(), innerKvp => innerKvp.Value.ToString()));
        _localSettingsService.SaveSettingAsync("chartSettings", serializableSettings);
    }

    public void ProcessTickValues(string details)
    {
        var entries = details.Split(", ");
        foreach (var entry in entries)
        {
            var parts = entry.Split(":");

            if (parts.Length == 2)
            {
                var symbolStr = parts[0];
                var symbol = (Symbol)Enum.Parse(typeof(Symbol), symbolStr);
                if (double.TryParse(parts[1], out var tickValue))
                {
                    _tickValues[symbol] = tickValue;
                }
                else
                {
                    throw new Exception($"Invalid tick value entry:{entry}");
                }
            }
            else
            {
                throw new Exception($"Invalid tick value entry:{entry}");
            }
        }
    }

    private static void RegisterChart(IDictionary<Tuple<Symbol, bool>, int> chartUsageCounts, Symbol symbol, bool isReversed)
    {
        var key = new Tuple<Symbol, bool>(symbol, isReversed);
        if (chartUsageCounts.TryGetValue(key, out var count))
        {
            chartUsageCounts[key] = count + 1;
        }
        else
        {
            chartUsageCounts[key] = 1;
        }
    }

    private static void UnRegisterChart(IDictionary<Tuple<Symbol, bool>, int> chartUsageCounts, Symbol symbol, bool isReversed)
    {
        var key = new Tuple<Symbol, bool>(symbol, isReversed);
        if (!chartUsageCounts.TryGetValue(key, out var count))
        {
            return;
        }

        count--;
        if (count <= 0)
        {
            chartUsageCounts.Remove(key);
        }
        else
        {
            chartUsageCounts[key] = count;
        }
    }
}

//private void LogNonZeroCounts()
//{
//    Debug.WriteLine($"--- --- --- --- --- --- --- ---");
//    LogNonZeroCountsInDictionary(_tickChartsCounter, nameof(_tickChartsCounter));
//    LogNonZeroCountsInDictionary(_candlestickChartsCounter, nameof(_candlestickChartsCounter));
//    LogNonZeroCountsInDictionary(_thresholdBarChartsCounter, nameof(_thresholdBarChartsCounter));
//}

//private static void LogNonZeroCountsInDictionary(Dictionary<Tuple<Symbol, bool>, int> dictionary, string dictionaryName)
//{
//    foreach (var entry in dictionary.Where(entry => entry.Value != 0))
//    {
//        System.Diagnostics.Debug.WriteLine($"In {dictionaryName}, Symbol {entry.Key.Item1} with IsReversed {entry.Key.Item2} has count {entry.Value}");
//    }
//}