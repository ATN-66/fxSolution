/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                    Quotations.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Models.Kernels;

public class Quotations : DataSourceKernel<Quotation>
{
    public Quotations(IFileService fileService) : base(fileService)
    {
    }

    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        Items.AddRange(quotations);
    }

    public override void Add(Quotation quotation)
    {
        Items.Add(quotation);
    }

    public override int FindIndex(DateTime dateTime)
    {
        throw new NotImplementedException("Quotations:FindIndex");
    }
    public override Quotation FindItem(DateTime givenTime)
    {
        throw new NotImplementedException("FindItem");
    }
    public override void SaveItems((DateTime first, DateTime second) dateRange)
    {
        throw new NotImplementedException("SaveItems");
    }

    public override void SaveForceTransformations()
    {
        throw new NotImplementedException("SaveForceTransformations");
    }
}