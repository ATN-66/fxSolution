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

    private readonly Dictionary<Symbol, int> _thresholdsInPips = new() { { Symbol.EURUSD, 18 }, { Symbol.GBPUSD, 30 }, { Symbol.USDJPY, 20 }, { Symbol.EURGBP, 30 }, { Symbol.EURJPY, 35 }, { Symbol.GBPJPY, 50 } };//todo:
    private readonly Dictionary<Symbol, int> _digits = new() { { Symbol.EURUSD, 100000 }, { Symbol.GBPUSD, 100000 }, { Symbol.USDJPY, 1000 }, { Symbol.EURGBP, 100000 }, { Symbol.EURJPY, 1000 }, { Symbol.GBPJPY, 1000 } };//todo

    private readonly Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> _dataSources = new();
    private readonly Dictionary<Symbol, Dictionary<ChartType, INotificationsKernel>> _notifications = new();

    public KernelService(IChartService chartService, IFileService fileService)
    {
        _chartService = chartService;
        _fileService = fileService;
    }

    public void Initialize(IDictionary<Symbol, List<Quotation>> quotations)
    {
        foreach (var (symbol, symbolQuotations) in quotations)
        {
            var symbolKernels = new Dictionary<ChartType, IDataSourceKernel<IChartItem>>();

            var thresholdKernel = new ThresholdBars(symbol, _thresholdsInPips[symbol], _digits[symbol], _fileService);
            thresholdKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.ThresholdBars] = thresholdKernel;

            var candlestickKernel = new Candlesticks(symbol, _fileService);
            candlestickKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Candlesticks] = candlestickKernel;

            var quotationKernel = new Quotations(_fileService);
            quotationKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Ticks] = quotationKernel;

            _dataSources[symbol] = symbolKernels;
        }

        // todo: initialize historical data to _notifications
        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            _notifications[symbol] = new Dictionary<ChartType, INotificationsKernel>();
            foreach (ChartType chartType in Enum.GetValues(typeof(ChartType)))
            {
                _notifications[symbol][chartType] = new Notifications(symbol, chartType);
            }
        }

        _chartService.Initialize(_dataSources, _notifications);
    }

    public void Add(Quotation quotation)
    {
        var symbol = quotation.Symbol;
        ((IDataSourceKernel<ThresholdBar>)_dataSources[symbol][ChartType.ThresholdBars]).Add(quotation);
        ((IDataSourceKernel<Candlestick>)_dataSources[symbol][ChartType.Candlesticks]).Add(quotation);
        ((IDataSourceKernel<Quotation>)_dataSources[symbol][ChartType.Ticks]).Add(quotation);
        _chartService.Tick(symbol);
    }
}