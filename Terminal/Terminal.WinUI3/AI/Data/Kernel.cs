/*+------------------------------------------------------------------+
  |                                           Terminal.WinUI3.AI.Data|
  |                                                        Kernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.AI.Data;

public abstract class Kernel<TItem> : IKernel<TItem> where TItem : IChartItem
{
    protected readonly List<TItem> Items = new();

    public int Count => Items.Count;

    public TItem this[int i]
    {
        get
        {
            if (i < 0 || i >= Items.Count)
            {
                throw new IndexOutOfRangeException($"Index {i} is out of range. There are only {Items.Count} items.");
            }

            return Items[Items.Count - 1 - i];
        }
    }

    public abstract void AddRange(IEnumerable<Quotation> quotations);
    public abstract void Add(Quotation quotation);
}