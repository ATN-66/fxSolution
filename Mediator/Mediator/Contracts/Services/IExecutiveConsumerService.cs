/*+------------------------------------------------------------------+
  |                                      Mediator.Contracts.Services |
  |                                     IExecutiveConsumerService.cs |
  +------------------------------------------------------------------+*/

namespace Mediator.Contracts.Services;

public interface IExecutiveConsumerService
{
    Task StartAsync(CancellationToken token);
}