﻿/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Contracts.Services |
  |                                                IChartService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Models.Chart;
using ChartControlBase = Terminal.WinUI3.Controls.Chart.Base.ChartControlBase;

namespace Terminal.WinUI3.Contracts.Services;

public interface IChartService
{
    Task InitializeAsync(Dictionary<Symbol, Dictionary<ChartType, IDataSourceKernel<IChartItem>>> dataSourceKernels, Dictionary<Symbol, INotificationsKernel> eventsKernels);
    void ProcessTickValues(string details);
    Task<ChartControlBase> GetDefaultChartAsync(Symbol symbol, bool isReversed);
    Task<ChartControlBase> GetChartByTypeAsync(Symbol symbol, bool isReversed, ChartType chartType);
    Task<T> GetChartAsync<T, TItem, TK>(Symbol symbol, ChartType chartType, bool isReversed) where T : Controls.Chart.ChartControl<TItem, TK> where TItem : IChartItem where TK : IDataSourceKernel<TItem>;
    void DisposeChart(ChartControlBase chartControlBase);
    void Tick(Symbol symbol);
}