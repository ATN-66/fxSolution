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
    private readonly IDictionary<Symbol, BaseChartControl?> _charts = new Dictionary<Symbol, BaseChartControl?>();
    private readonly IDictionary<Symbol, BaseChartControl?> _chartsOpposite = new Dictionary<Symbol, BaseChartControl?>();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    public void Initialize(IDictionary<Symbol, Kernel> kernels)
    {
        _kernels = kernels;
    }

    public BaseChartControl? GetChartControl(Symbol symbol, bool isOpposite)
    {
        if (isOpposite)
        {
            _chartsOpposite[symbol] = new BaseChartControl(_kernels[symbol], symbol, isOpposite);
            return _chartsOpposite[symbol];
        }
        else
        {
            _charts[symbol] = new BaseChartControl(_kernels[symbol], symbol, isOpposite);
            return _charts[symbol];
        }
    }

    public void Tick()
    {
        //notify chart control to update
    }
}