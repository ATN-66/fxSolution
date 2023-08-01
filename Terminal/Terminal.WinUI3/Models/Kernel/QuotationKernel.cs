/*+------------------------------------------------------------------+
  |                                           Terminal.WinUI3.AI.Data|
  |                                               QuotationKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Kernel;

public class QuotationKernel : Kernel<Quotation>
{
    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        Items.AddRange(quotations);
    }

    public override void Add(Quotation quotation)
    {
        Items.Add(quotation);
    }
}