/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                           IExternalDataSource.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IExternalDataSource
{
    Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider = Provider.Mediator, bool exactly = false);
}
