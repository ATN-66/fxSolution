/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                        Kernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.AI.Data;

public class Kernel
{
    private readonly Symbol _symbol;
    private readonly List<Quotation> _quotations = new();

    //collection to keep candlesticks
    //collection to keep any other data

    public Kernel(Symbol symbol)
    {
        _symbol = symbol;
    }

    public void AddRange(IEnumerable<Quotation> quotations)
    {
        _quotations.AddRange(quotations);
    }

    public void Add(Quotation quotation)
    {
        _quotations.Add(quotation);
    }

    public int Count => _quotations.Count;

    public Quotation this[int i]
    {
        get
        {
            if (i < 0 || i >= _quotations.Count)
            {
                throw new IndexOutOfRangeException($"Index {i} is out of range. There are only {_quotations.Count} quotations.");
            }

            return _quotations[_quotations.Count - 1 - i];
        }
    }
}