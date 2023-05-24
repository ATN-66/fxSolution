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
    BaseChartControl GetChartControl(Symbol symbol);
    void Tick();
}