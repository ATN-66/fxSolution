/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Quotation = Common.Entities.Quotation;

namespace Mediator.Contracts.Services;

public interface IDataService
{
    Task SaveQuotationsAsync(List<Quotation> quotations);
    Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime);
}