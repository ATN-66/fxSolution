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
    Task InitializeAsync(Dictionary<Symbol, Dictionary<ChartType, IKernel>> kernels);
    T GetChart<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : ChartControl<TItem, TK> where TItem : IChartItem where TK : IKernel<TItem>;
    void DisposeChart(ChartControlBase chartControlBase);
    void Tick(Symbol symbol);
    ChartControlBase GetDefaultChart(Symbol symbol, bool isReversed);
}