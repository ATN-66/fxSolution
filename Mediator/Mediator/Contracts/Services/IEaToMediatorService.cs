/*+------------------------------------------------------------------+
  |                                      Mediator.Contracts.Services |
  |                                   IEaToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Contracts.Services;

internal interface IEaToMediatorService
{
    Task StartAsync(Symbol symbol, CancellationToken token);
}