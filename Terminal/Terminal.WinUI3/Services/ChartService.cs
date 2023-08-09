/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Services |
  |                                                  ChartService.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Windows.UI;
using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Controls.Chart.Candlestick;
using Terminal.WinUI3.Controls.Chart.ThresholdBar;
using Terminal.WinUI3.Controls.Chart.Tick;
using Terminal.WinUI3.Models.Entities;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Kernels;
using ChartControlBase = Terminal.WinUI3.Controls.Chart.Base.ChartControlBase;

namespace Terminal.WinUI3.Services;

public class ChartService : IChartService
{
    private readonly IConfiguration _configuration;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IDispatcherService _dispatcherService;

    private Dictionary<Symbol, Dictionary<bool, Dictionary<ChartType, ChartSettings>>> _settings = null!;
    private Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> _dataSources = null!;
    private Dictionary<Symbol, INotificationsKernel> _notifications = new();

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

    public void Initialize(Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> dataSourceKernels, Dictionary<Symbol, INotificationsKernel> eventsKernels)
    {
        _dataSources = dataSourceKernels;
        _notifications = eventsKernels;

        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _tickChartsReversed[(Symbol)symbol] = null;
            _tickCharts[(Symbol)symbol] = null;
            _candlestickChartsReversed[(Symbol)symbol] = null;
            _candlestickCharts[(Symbol)symbol] = null;
            _thresholdBarChartsReversed[(Symbol)symbol] = null;
            _thresholdBarCharts[(Symbol)symbol] = null;
        }

        LoadSettingsAsync();
    }

    public async Task<ChartControlBase> GetDefaultChartAsync(Symbol symbol, bool isReversed)
    {
        ChartType chartType;

        if (_settings.ContainsKey(symbol) && _settings[symbol].ContainsKey(isReversed))
        {
            var settingsForSymbolAndReversed = _settings[symbol][isReversed];
            if (settingsForSymbolAndReversed.Any(s => s.Value.IsDefault))
            {
                chartType = settingsForSymbolAndReversed.First(s => s.Value.IsDefault).Key;
            }
            else
            {
                chartType = ChartType.Candlesticks;
            }
        }
        else
        {
            chartType = ChartType.Candlesticks;
        }

        return chartType switch
        {
            ChartType.Ticks => await GetChartAsync<TickChartControl, Quotation, Quotations>(symbol, isReversed, chartType).ConfigureAwait(false),
            ChartType.Candlesticks => await GetChartAsync<CandlestickChartControl, Candlestick, Candlesticks>(symbol, isReversed, chartType).ConfigureAwait(false),
            ChartType.ThresholdBars => await GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBars>(symbol, isReversed, chartType).ConfigureAwait(false),
            _ => throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType))
        };
    }

    public async Task<ChartControlBase> GetChartByTypeAsync(Symbol symbol, bool isReversed, ChartType chartType)
    {
        return chartType switch
        {
            ChartType.Ticks => await GetChartAsync<TickChartControl, Quotation, Quotations>(symbol, isReversed, chartType).ConfigureAwait(false),
            ChartType.Candlesticks => await GetChartAsync<CandlestickChartControl, Candlestick, Candlesticks>(symbol, isReversed, chartType).ConfigureAwait(false),
            ChartType.ThresholdBars => await GetChartAsync<ThresholdBarChartControl, ThresholdBar, ThresholdBars>(symbol, isReversed, chartType).ConfigureAwait(false),
            _ => throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType))
        };
    }

    public async Task<T> GetChartAsync<T, TItem, TK>(Symbol symbol,  bool isReversed, ChartType chartType) where T : Controls.Chart.ChartControl<TItem, TK> where TItem : IChartItem where TK : IDataSourceKernel<TItem>
    {
        T result;
        var settings = _settings[symbol][isReversed][chartType];
        ValidateSettings(ref settings);
        Debug.Assert(settings.Symbol == symbol);
        Debug.Assert(settings.IsReversed == isReversed);
        Debug.Assert(settings.ChartType == chartType);

        var notifications = _notifications[symbol];

        switch (chartType)
        {
            case ChartType.Ticks:
                var quotations = _dataSources[symbol][ChartType.Ticks] as Quotations ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_tickChartsReversed[symbol] != null)
                    {
                        DisposeChart(_tickChartsReversed[symbol]!.GetChartSettings(), true);
                        _tickChartsReversed[symbol] = null;
                    }

                    _tickChartsReversed[symbol] = new TickChartControl(_configuration, settings, _tickValues[symbol], quotations, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_tickChartsReversed[symbol] as T)!;
                    RegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_tickCharts[symbol] != null)
                    {
                        DisposeChart(_tickCharts[symbol]!.GetChartSettings(), true);
                        _tickCharts[symbol] = null;
                    }
                    _tickCharts[symbol] = new TickChartControl(_configuration, settings, _tickValues[symbol], quotations, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_tickCharts[symbol] as T)!;
                    RegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.Candlesticks:
                var candlesticks = _dataSources[symbol][ChartType.Candlesticks] as Candlesticks ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_candlestickChartsReversed[symbol] != null)
                    {
                        DisposeChart(_candlestickChartsReversed[symbol]!.GetChartSettings(), true);
                        _candlestickChartsReversed[symbol] = null;
                    }
                    _candlestickChartsReversed[symbol] = new CandlestickChartControl(_configuration, settings, _tickValues[symbol], candlesticks, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_candlestickChartsReversed[symbol] as T)!;
                    RegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_candlestickCharts[symbol] != null)
                    {
                        DisposeChart(_candlestickCharts[symbol]!.GetChartSettings(), true);
                        _candlestickCharts[symbol] = null;
                    }
                    _candlestickCharts[symbol] = new CandlestickChartControl(_configuration, settings, _tickValues[symbol], candlesticks, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_candlestickCharts[symbol] as T)!;
                    RegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.ThresholdBars:
                var thresholdBars = _dataSources[symbol][ChartType.ThresholdBars] as ThresholdBars ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_thresholdBarChartsReversed[symbol] != null)
                    {
                        DisposeChart(_thresholdBarChartsReversed[symbol]!.GetChartSettings(), true);
                        _thresholdBarChartsReversed[symbol] = null;
                    }
                    _thresholdBarChartsReversed[symbol] = new ThresholdBarChartControl(_configuration, settings, _tickValues[symbol], thresholdBars, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_thresholdBarChartsReversed[symbol] as T)!;
                    RegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_thresholdBarCharts[symbol] != null)
                    {
                        DisposeChart(_thresholdBarCharts[symbol]!.GetChartSettings(), true);
                        _thresholdBarCharts[symbol] = null;
                    }
                    _thresholdBarCharts[symbol] = new ThresholdBarChartControl(_configuration, settings, _tickValues[symbol], thresholdBars, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_thresholdBarCharts[symbol] as T)!;
                    RegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                break;
            default: throw new ArgumentException($@"Unsupported chart type {chartType}", nameof(chartType));
        }
        await result.InitializeAsync().ConfigureAwait(false);
        return result;
    }

    public void DisposeChart(ChartSettings settings, bool isDefault)
    {
        settings.IsDefault = isDefault;

        var symbol = settings.Symbol;
        var isReversed = settings.IsReversed;
        var chartType = settings.ChartType;
        ValidateSettings(ref settings);
        _settings[symbol][isReversed][chartType] = settings;

        switch (chartType)
        {
            case ChartType.Ticks:
                if (_tickChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
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

                    UnRegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.Candlesticks:
                if (_candlestickChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
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

                    UnRegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                break;
            case ChartType.ThresholdBars:
                if (_thresholdBarChartsCounter[new Tuple<Symbol, bool>(symbol, isReversed)] == 1)
                {
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

                    UnRegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                break;
            default: throw new ArgumentException($@"Unsupported chart type {settings.ChartType}", nameof(settings.ChartType));
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

    private void LoadSettingsAsync()
    {
        var serializableSettings = _localSettingsService.ReadSetting<Dictionary<string, Dictionary<string, Dictionary<string, ChartSettings>>>>("ChartsSettings");

        if (serializableSettings != null)
        {
            _settings = serializableSettings.ToDictionary(
                kvp => Enum.Parse<Symbol>(kvp.Key),
                kvp => kvp.Value.ToDictionary(
                    innerKvp => bool.Parse(innerKvp.Key),
                    innerKvp => innerKvp.Value.ToDictionary(
                        innerInnerKvp => Enum.Parse<ChartType>(innerInnerKvp.Key),
                        innerInnerKvp => innerInnerKvp.Value
                    )
                )
            );
        }
        else
        {
            _settings = new Dictionary<Symbol, Dictionary<bool, Dictionary<ChartType, ChartSettings>>>();
            foreach (var symbol in Enum.GetValues(typeof(Symbol)))
            {
                _settings[(Symbol)symbol] = new Dictionary<bool, Dictionary<ChartType, ChartSettings>>();
                foreach (var isReversed in new[] { false, true })
                {
                    _settings[(Symbol)symbol][isReversed] = new Dictionary<ChartType, ChartSettings>();
                    foreach (var chartType in Enum.GetValues(typeof(ChartType)))
                    {
                        _settings[(Symbol)symbol][isReversed][(ChartType)chartType] = new ChartSettings
                        {
                            IsDefault = (ChartType)chartType == ChartType.Candlesticks,
                            Symbol = (Symbol)symbol,
                            IsReversed = isReversed,
                            ChartType = (ChartType)chartType,
                            HorizontalShift = _configuration.GetValue<int>("_horizontalShiftDefault"),
                            VerticalShift = _configuration.GetValue<int>("_verticalShiftDefault"),
                            KernelShiftPercent = _configuration.GetValue<int>("_kernelShiftPercentDefault")
                        };
                    }
                }
            }
        }
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

    private static void ValidateSettings(ref ChartSettings settings)
    {
        if (settings.HorizontalShift > 0 && settings.KernelShiftPercent != 100)
        {
            settings.KernelShiftPercent = 100;
        }
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        foreach (var pair in _tickChartsReversed.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }
        foreach (var pair in _tickCharts.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }

        foreach (var pair in _candlestickChartsReversed.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }
        foreach (var pair in _candlestickCharts.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }

        foreach (var pair in _thresholdBarChartsReversed.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }
        foreach (var pair in _thresholdBarCharts.Where(pair => pair.Value != null))
        {
            DisposeChart(pair.Value!.GetChartSettings(), true);
        }

        SaveSettingsAsync();
    }

    private void SaveSettingsAsync()
    {
        var serializableSettings = _settings.ToDictionary(kvp =>
            kvp.Key.ToString(), kvp => kvp.Value.ToDictionary(
            innerKvp => innerKvp.Key.ToString(), innerKvp => innerKvp.Value.ToDictionary(
                innerInnerKvp => innerInnerKvp.Key.ToString(), innerInnerKvp => innerInnerKvp.Value)));

        _localSettingsService.SaveSetting("ChartsSettings", serializableSettings);
    }
}