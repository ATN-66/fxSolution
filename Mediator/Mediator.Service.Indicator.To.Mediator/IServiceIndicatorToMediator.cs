/*+------------------------------------------------------------------+
  |                            Mediator.Server.Indicator.To.Mediator |
  |                                   IServiceIndicatorToMediator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Mediator.Service.Indicator.To.Mediator;

public interface IServiceIndicatorToMediator
{
    Task StartAsync(Symbol symbol, CancellationToken cancellationToken);
}