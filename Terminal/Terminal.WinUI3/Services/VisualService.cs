/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Services |
  |                                                 VisualService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Services;

public class VisualService : IVisualService
{
    private readonly IDictionary<Symbol, BaseChartControl> _charts = new Dictionary<Symbol, BaseChartControl>();

    public void Initialize()
    {

    }

    public BaseChartControl GetChartControl(Symbol symbol)
    {
        _charts[symbol] = new BaseChartControl();
        return _charts[symbol];
    }

    public void Tick()
    {
        //notify chart control to update
    }
}