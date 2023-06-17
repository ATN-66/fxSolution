/*+------------------------------------------------------------------+
  |                                      Mediator.Contracts.Services |
  |                                   IIndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Contracts.Services;

internal interface IIndicatorToMediatorService
{
    Task StartAsync(Symbol symbol, CancellationToken cancellationToken);
}