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

public interface IKernel<TItem> : IKernel where TItem : IChartItem 
{
    TItem this[int i] { get; }
    void AddRange(IEnumerable<Quotation> quotations); 
    void Add(Quotation quotation);
}