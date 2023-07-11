/*+------------------------------------------------------------------+
  |                                      Mediator.Contracts.Services |
  |                                   IDataConsumerService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Contracts.Services;

internal interface IDataConsumerService
{
    Task StartAsync(Symbol symbol, CancellationToken token);
}