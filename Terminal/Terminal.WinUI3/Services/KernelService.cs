/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                 KernelService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Entities;
using Terminal.WinUI3.Models.Kernels;

namespace Terminal.WinUI3.Services;

public class KernelService : IKernelService
{
    private readonly IChartService _chartService;
    private readonly IFileService _fileService;

    private readonly Dictionary<Symbol, int> _thresholdsInPips = new() { { Symbol.EURUSD, 20 }, { Symbol.GBPUSD, 30 }, { Symbol.USDJPY, 20 }, { Symbol.EURGBP, 30 }, { Symbol.EURJPY, 40 }, { Symbol.GBPJPY, 60 } };//todo:
    private readonly Dictionary<Symbol, int> _digits = new() { { Symbol.EURUSD, 100000 }, { Symbol.GBPUSD, 100000 }, { Symbol.USDJPY, 1000 }, { Symbol.EURGBP, 100000 }, { Symbol.EURJPY, 1000 }, { Symbol.GBPJPY, 100000 } };//todo

    private readonly Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> _dataSourceKernels = new();
    private readonly Dictionary<Symbol, INotificationsKernel> _notificationsKernels = new();

    public KernelService(IChartService chartService, IFileService fileService)
    {
        _chartService = chartService;
        _fileService = fileService;
    }

    public Task InitializeAsync(IDictionary<Symbol, List<Quotation>> quotations)
    {
        foreach (var (symbol, symbolQuotations) in quotations)
        {
            var symbolKernels = new Dictionary<ChartType, IDataSourceKernel<IChartItem>>();

            var thresholdKernel = new ThresholdBars(_thresholdsInPips[symbol], _digits[symbol]);
            thresholdKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.ThresholdBars] = thresholdKernel;

            var candlestickKernel = new Candlesticks();
            candlestickKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Candlesticks] = candlestickKernel;

            var quotationKernel = new Quotations();
            quotationKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Ticks] = quotationKernel;

            _dataSourceKernels[symbol] = symbolKernels;
        }

        // todo: initialize historical data to _notificationsKernels
        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            _notificationsKernels[symbol] = new Notifications();
        }

        return _chartService.InitializeAsync(_dataSourceKernels, _notificationsKernels);
    }

    public void Add(Quotation quotation)
    {
        var symbol = quotation.Symbol;
        ((IDataSourceKernel<ThresholdBar>)_dataSourceKernels[symbol][ChartType.ThresholdBars]).Add(quotation);
        ((IDataSourceKernel<Candlestick>)_dataSourceKernels[symbol][ChartType.Candlesticks]).Add(quotation);
        ((IDataSourceKernel<Quotation>)_dataSourceKernels[symbol][ChartType.Ticks]).Add(quotation);
        _chartService.Tick(symbol);
    }
}