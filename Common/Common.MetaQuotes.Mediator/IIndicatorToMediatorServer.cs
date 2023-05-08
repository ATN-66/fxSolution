/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                    IIndicatorToMediatorServer.cs |
  +------------------------------------------------------------------+*/

using System.Threading;
using System.Threading.Tasks;
using Common.Entities;

namespace Common.MetaQuotes.Mediator;

public interface IIndicatorToMediatorServer
{
    Task StartAsync(Symbol symbol, CancellationToken cancellationToken);
}