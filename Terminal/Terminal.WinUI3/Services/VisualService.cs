/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Services |
  |                                                 VisualService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Services;

public class VisualService : IVisualService
{
    private readonly IDispatcherService _dispatcherService;

    private readonly Dictionary<Symbol, TickChartControl?> _tickCharts = new();
    private readonly Dictionary<Symbol, TickChartControl?> _tickChartsReversed = new();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    public VisualService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    public void Initialize(IDictionary<Symbol, Kernel> kernels)
    {
        _kernels = kernels;

        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _tickChartsReversed[(Symbol)symbol] = null!;
            _tickCharts[(Symbol)symbol] = null!;
        }
    }

    public TickChartControl GetTickChartControl(Symbol symbol, bool isReversed)
    {
        if (isReversed)
        {
            _tickChartsReversed[symbol] = new TickChartControl(_kernels[symbol], symbol, true);
            return _tickChartsReversed[symbol]!;
        }

        _tickCharts[symbol] = new TickChartControl(_kernels[symbol], symbol, false);
        return _tickCharts[symbol]!;
    }

    public void Tick(Symbol symbol)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            _tickCharts[symbol]?.Tick();
            _tickChartsReversed[symbol]?.Tick();
        });
    }
}