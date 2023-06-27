/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Quotation = Common.Entities.Quotation;

namespace Mediator.Contracts.Services;

public interface IDataService
{
    Workplace Workplace { get; set; }
    Task SaveQuotationsAsync(List<Quotation> quotations);
    Task<IEnumerable<Quotation>> GetSinceDateTimeHourTillNowAsync(DateTime startDateTime);
    Task<Dictionary<ActionResult, int>> BackupAsync();
}