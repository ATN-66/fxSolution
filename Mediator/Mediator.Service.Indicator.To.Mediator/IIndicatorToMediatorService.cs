/*+------------------------------------------------------------------+
  |                            Mediator.Server.Indicator.To.Mediator |
  |                                    IIndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Service.Indicator.To.Mediator;

public interface IIndicatorToMediatorService
{
    Task StartAsync(Symbol symbol, CancellationToken cancellationToken);
}