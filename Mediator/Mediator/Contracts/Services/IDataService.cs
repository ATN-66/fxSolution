/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Contracts.Services;

public interface IDataService
{
    Task SaveQuotationsAsync(List<Quotation> quotations);
}