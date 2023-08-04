/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Contracts.Models|
  |                                             IDataSourceKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Contracts.Models;

public interface IDataSourceKernel<out TItem> : IKernel where TItem : IChartItem
{
    int Count { get; }
    TItem this[int i] { get; }
    void AddRange(IEnumerable<Quotation> quotations);
    void Add(Quotation quotation);
}