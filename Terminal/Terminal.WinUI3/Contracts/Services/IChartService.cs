/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Contracts.Services |
  |                                                IChartService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.Contracts.Services;

public interface IChartService
{
    Task InitializeAsync(Dictionary<Symbol, Dictionary<ChartType, IKernel>> kernels);
    void ProcessTickValues(string details);
    Task<ChartControlBase> GetDefaultChartAsync(Symbol symbol, bool isReversed);
    Task<ChartControlBase> GetChartByTypeAsync(Symbol symbol, bool isReversed, ChartType chartType);
    Task<T> GetChartAsync<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : ChartControl<TItem, TK> where TItem : IChartItem where TK : IKernel<TItem>;
    void DisposeChart(ChartControlBase chartControlBase);
    void Tick(Symbol symbol);
}