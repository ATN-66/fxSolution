/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Contracts.Services |
  |                                                IVisualService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Contracts.Services;

public interface IVisualService
{
    void Initialize(IDictionary<Symbol, Kernel> kernels);
    TickChartControl? GetTickChartControl(Symbol symbol, bool isOpposite);
    void Tick();
}