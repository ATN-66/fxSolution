/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                              DataSource.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.Models.Kernels;

public abstract class DataSourceKernel<TItem> : IDataSourceKernel<TItem> where TItem : IChartItem
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