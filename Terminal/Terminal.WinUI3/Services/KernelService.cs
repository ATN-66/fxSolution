/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                 KernelService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Entities;
using Terminal.WinUI3.Models.Kernel;

namespace Terminal.WinUI3.Services;

public class KernelService : IKernelService
{
    private readonly IChartService _chartService;
    private readonly Dictionary<Symbol, int> _digits = new() { { Symbol.EURUSD, 100000 }, { Symbol.GBPUSD, 100000 }, { Symbol.USDJPY, 1000 }, { Symbol.EURGBP, 100000 }, { Symbol.EURJPY, 1000 }, { Symbol.GBPJPY, 100000 } };
    private readonly Dictionary<Symbol, int> _thresholdsInPips = new() { { Symbol.EURUSD, 20 }, { Symbol.GBPUSD, 20 }, { Symbol.USDJPY, 20 }, { Symbol.EURGBP, 20 }, { Symbol.EURJPY, 20 }, { Symbol.GBPJPY, 20 } };
    private readonly Dictionary<Symbol, Dictionary<ChartType, IKernel>> _kernels = new();

    public KernelService(IChartService chartService)
    {
        _chartService = chartService;
    }

    public Task InitializeAsync(IDictionary<Symbol, List<Quotation>> quotations)
    {
        foreach (var (symbol, symbolQuotations) in quotations)
        {
            var symbolKernels = new Dictionary<ChartType, IKernel>();

            var thresholdKernel = new ThresholdBarKernel(_thresholdsInPips[symbol], _digits[symbol]);
            thresholdKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.ThresholdBar] = thresholdKernel;

            var candlestickKernel = new CandlestickKernel();
            candlestickKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Candlesticks] = candlestickKernel;

            var quotationKernel = new QuotationKernel();
            quotationKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Ticks] = quotationKernel;

            _kernels[symbol] = symbolKernels;
        }

        return _chartService.InitializeAsync(_kernels);
    }

    public void Add(Quotation quotation)
    {
        var symbol = quotation.Symbol;
        ((IKernel<ThresholdBar>)_kernels[symbol][ChartType.ThresholdBar]).Add(quotation);
        ((IKernel<Candlestick>)_kernels[symbol][ChartType.Candlesticks]).Add(quotation);
        ((IKernel<Quotation>)_kernels[symbol][ChartType.Ticks]).Add(quotation);
        _chartService.Tick(symbol);
    }
}