/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                 KernelManager.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.AI.Data;

public class KernelManager : IKernelManager
{
    private readonly IVisualService _visualService;
    private readonly Dictionary<Symbol, Dictionary<ChartType, IKernel>> _kernels = new();

    public KernelManager(IVisualService visualService)
    {
        _visualService = visualService;
    }

    public void Initialize(IDictionary<Symbol, List<Quotation>> quotations)
    {
        foreach (var (symbol, symbolQuotations) in quotations)
        {
            var symbolKernels = new Dictionary<ChartType, IKernel>();

            var candlestickKernel = new CandlestickKernel();
            candlestickKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Candlesticks] = candlestickKernel;

            var quotationKernel = new QuotationKernel();
            quotationKernel.AddRange(symbolQuotations);
            symbolKernels[ChartType.Ticks] = quotationKernel;

            _kernels[symbol] = symbolKernels;
        }

        _visualService.Initialize(_kernels);
    }

    public void Add(Quotation quotation)
    {
        return;
        var symbol = quotation.Symbol;
        ((IKernel<Candlestick>)_kernels[symbol][ChartType.Candlesticks]).Add(quotation);
        ((IKernel<Quotation>)_kernels[symbol][ChartType.Ticks]).Add(quotation);
        _visualService.Tick(symbol);
    }
}