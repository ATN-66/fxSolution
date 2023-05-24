/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.AI.Data |
  |                                                        Kernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.AI.Data;

public class Kernel
{
    private Symbol _symbol;
    private readonly IList<Quotation> _quotations = new List<Quotation>();

    public Kernel(Symbol symbol)
    {
        _symbol = symbol;
    }

    public void Initialize(Quotation quotation)
    {
        _quotations.Add(quotation);
    }

    public void Tick(Quotation quotation)
    {
        _quotations.Add(quotation);
    }
}