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
    private Dictionary<Symbol, Dictionary<ChartType, INotificationsKernel>> _notifications = new();

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

    public void Initialize(Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> dataSourceKernels, Dictionary<Symbol, Dictionary<ChartType, INotificationsKernel>> notificationsKernels)
    {
        _dataSources = dataSourceKernels;
        _notifications = notificationsKernels;

        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _tickChartsReversed[(Symbol)symbol] = null;
            _tickCharts[(Symbol)symbol] = null;
            _candlestickChartsReversed[(Symbol)symbol] = null;
            _candlestickCharts[(Symbol)symbol] = null;
            _thresholdBarChartsReversed[(Symbol)symbol] = null;
            _thresholdBarCharts[(Symbol)symbol] = null;

            foreach (var isReversed in new[] { false, true })
            {
                var key = Tuple.Create((Symbol)symbol, isReversed);
                _tickChartsCounter[key] = 0;
                _candlestickChartsCounter[key] = 0;
                _thresholdBarChartsCounter[key] = 0;
            }
        }

        LoadSettingsAsync();
    }

    public async Task<ChartControlBase> GetDefaultChartAsync(Symbol symbol, bool isReversed)
    {
        ChartType chartType;

        try
        {
            if (_settings.ContainsKey(symbol) && _settings[symbol].ContainsKey(isReversed))
            {
                var settingsForSymbolAndReversed = _settings[symbol][isReversed];
                chartType = settingsForSymbolAndReversed.Any(s => s.Value.IsDefault) ? settingsForSymbolAndReversed.First(s => s.Value.IsDefault).Key : ChartType.Candlesticks;
            }
            else
            {
                chartType = ChartType.Candlesticks;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
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
        ChartSettings settings;

        var notifications = _notifications[symbol][chartType];

        switch (chartType)
        {
            case ChartType.Ticks:
                var quotations = _dataSources[symbol][ChartType.Ticks] as Quotations ?? throw new InvalidCastException();
                if (isReversed)
                {
                    if (_tickChartsReversed[symbol] != null)
                    {
                        settings = _tickChartsReversed[symbol]!.GetChartSettings();
                        _tickChartsReversed[symbol]!.Dispose();
                        _tickChartsReversed[symbol] = null;
                    }
                    else
                    {
                        settings = LoadSettings();
                    }
                    _tickChartsReversed[symbol] = new TickChartControl(_configuration, settings, _tickValues[symbol], quotations, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_tickChartsReversed[symbol] as T)!;
                    RegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_tickCharts[symbol] != null)
                    {
                        settings = _tickCharts[symbol]!.GetChartSettings();
                        _tickCharts[symbol]!.Dispose();
                        _tickCharts[symbol] = null;
                    }
                    else
                    {
                        settings = LoadSettings();
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
                        settings = _candlestickChartsReversed[symbol]!.GetChartSettings();
                        _candlestickChartsReversed[symbol]!.Dispose();
                        _candlestickChartsReversed[symbol] = null;
                    }else
                    {
                        settings = LoadSettings();
                    }
                    _candlestickChartsReversed[symbol] = new CandlestickChartControl(_configuration, settings, _tickValues[symbol], candlesticks, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_candlestickChartsReversed[symbol] as T)!;
                    RegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_candlestickCharts[symbol] != null)
                    {
                        settings = _candlestickCharts[symbol]!.GetChartSettings();
                        _candlestickCharts[symbol]!.Dispose();
                        _candlestickCharts[symbol] = null;
                    }
                    else
                    {
                        settings = LoadSettings();
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
                        settings = _thresholdBarChartsReversed[symbol]!.GetChartSettings();
                        _thresholdBarChartsReversed[symbol]!.Dispose();
                        _thresholdBarChartsReversed[symbol] = null;
                    }
                    else
                    {
                        settings = LoadSettings();
                    }
                    _thresholdBarChartsReversed[symbol] = new ThresholdBarChartControl(_configuration, settings, _tickValues[symbol], thresholdBars, notifications, _symbolColorsMap[symbol].BaseColor, _symbolColorsMap[symbol].QuoteColor, App.GetService<ILogger<ChartControlBase>>());
                    result = (_thresholdBarChartsReversed[symbol] as T)!;
                    RegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                else
                {
                    if (_thresholdBarCharts[symbol] != null)
                    {
                        //DisposeChart(_thresholdBarCharts[symbol]!.GetChartSettings(), true);
                        settings = _thresholdBarCharts[symbol]!.GetChartSettings();
                        _thresholdBarCharts[symbol] = null;
                    }
                    else
                    {
                        settings = LoadSettings();
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

        ChartSettings LoadSettings()
        {
            try
            {
                settings = _settings[symbol][isReversed][chartType];
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }

            ValidateSettings(ref settings);
            Debug.Assert(settings.Symbol == symbol);
            Debug.Assert(settings.IsReversed == isReversed);
            Debug.Assert(settings.ChartType == chartType);
            return settings;
        }
    }

    public void DisposeChart(ChartSettings settings, bool isDefault)
    {
        settings.IsDefault = isDefault;
        var symbol = settings.Symbol;
        var isReversed = settings.IsReversed;
        var chartType = settings.ChartType;
        ValidateSettings(ref settings);

        try
        {
            _settings[symbol][isReversed][chartType] = settings;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }

        switch (chartType)
        {
            case ChartType.Ticks:
                try
                {
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
                    }

                    UnRegisterChart(_tickChartsCounter, symbol, isReversed);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw;
                }
                break;
            case ChartType.Candlesticks:
                try
                {
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
                    }

                    UnRegisterChart(_candlestickChartsCounter, symbol, isReversed);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw;
                }
                break;
            case ChartType.ThresholdBars:
                try
                {
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
                    }

                    UnRegisterChart(_thresholdBarChartsCounter, symbol, isReversed);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw;
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
        chartUsageCounts[key] += 1;
    }

    private static void UnRegisterChart(IDictionary<Tuple<Symbol, bool>, int> chartUsageCounts, Symbol symbol, bool isReversed)
    {
        var key = new Tuple<Symbol, bool>(symbol, isReversed);
        if (!chartUsageCounts.TryGetValue(key, out var count))
        {
            return;
        }

        count--;
        if (count < 0)
        {
            count = 0;
        }

        chartUsageCounts[key] = count;
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

    public void ListAllRegisteredCharts()
    {
        var result = new List<(Symbol Symbol, bool IsReversed, ChartType ChartType)>();
        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            foreach (var isReversed in new[] { false, true })
            {
                result.AddRange(from ChartType chartType in Enum.GetValues(typeof(ChartType))
                    let count = chartType switch
                    {
                        ChartType.Ticks => _tickChartsCounter[Tuple.Create(symbol, isReversed)],
                        ChartType.Candlesticks => _candlestickChartsCounter[Tuple.Create(symbol, isReversed)],
                        ChartType.ThresholdBars => _thresholdBarChartsCounter[Tuple.Create(symbol, isReversed)],
                        _ => 0 // or throw an exception for unsupported types
                    }
                    where count > 0
                    select (symbol, isReversed, chartType));
            }
        }

        Debug.WriteLine("--- All Registered Charts: ---");
        foreach (var chart in result)
        {
            Debug.WriteLine($"Symbol: {chart.Symbol}, IsReversed: {chart.IsReversed}, ChartType: {chart.ChartType}");
        }
        Debug.WriteLine("--- done ---");
    }

    public void ListAllNonNullCharts()
    {
        var result = new List<(Symbol Symbol, bool IsReversed, ChartType ChartType)>();

        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            if (_tickChartsReversed[symbol] != null)
                result.Add((symbol, true, ChartType.Ticks));
            if (_tickCharts[symbol] != null)
                result.Add((symbol, false, ChartType.Ticks));

            if (_candlestickChartsReversed[symbol] != null)
                result.Add((symbol, true, ChartType.Candlesticks));
            if (_candlestickCharts[symbol] != null)
                result.Add((symbol, false, ChartType.Candlesticks));

            if (_thresholdBarChartsReversed[symbol] != null)
                result.Add((symbol, true, ChartType.ThresholdBars));
            if (_thresholdBarCharts[symbol] != null)
                result.Add((symbol, false, ChartType.ThresholdBars));
        }

        Debug.WriteLine("--- All Non-Null Charts: ---");
        foreach (var chart in result)
        {
            Debug.WriteLine($"Symbol: {chart.Symbol}, IsReversed: {chart.IsReversed}, ChartType: {chart.ChartType}");
        }
        Debug.WriteLine("--- done ---");
    }
}