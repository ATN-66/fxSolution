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
    private readonly IDictionary<Symbol, TickChartControl> _tickCharts = new Dictionary<Symbol, TickChartControl>();
    private readonly IDictionary<Symbol, TickChartControl> _tickChartsReversed = new Dictionary<Symbol, TickChartControl>();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    public void Initialize(IDictionary<Symbol, Kernel> kernels)
    {
        _kernels = kernels;
    }

    public TickChartControl GetTickChartControl(Symbol symbol, bool isOpposite)
    {
        if (isOpposite)
        {
            _tickChartsReversed[symbol] = new TickChartControl(_kernels[symbol], symbol, isOpposite);
            return _tickChartsReversed[symbol];
        }
        else
        {
            _tickCharts[symbol] = new TickChartControl(_kernels[symbol], symbol, isOpposite);
            return _tickCharts[symbol];
        }
    }

    public void Tick()
    {
        //notify chart control to update
    }
}