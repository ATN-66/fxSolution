/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                    Quotations.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Kernels;

public class Quotations : DataSourceKernel<Quotation>
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