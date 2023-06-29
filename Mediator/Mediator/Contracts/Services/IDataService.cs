/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Quotation = Common.Entities.Quotation;

namespace Mediator.Contracts.Services;

public interface IDataService
{
    Task<int> SaveDataAsync(IEnumerable<Quotation> quotations);
    Task<IEnumerable<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive);
    Task<Dictionary<ActionResult, int>> BackupAsync();
}