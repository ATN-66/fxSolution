/*+------------------------------------------------------------------+
  |                                           Terminal.WinUI3.AI.Data|
  |                                                       IKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.AI.Data;

public interface IKernel
{
    int Count { get; }
}

public interface IKernel<TItem> : IKernel where TItem : IChartItem // todo:
{
    TItem this[int i] { get; }
    void AddRange(IEnumerable<Quotation> quotations); // todo:
    void Add(Quotation quotation);
}