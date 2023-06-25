/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                     ITicksDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Mediator.Models;
using Ticksdata;

namespace Mediator.Contracts.Services;

public interface ITicksDataProviderService
{
    event EventHandler<ActivationChangedEventArgs> IsActivatedChanged;
    bool IsActivated { get; set; }
    Task StartAsync();
    Task GetTicksAsync(GetTicksRequest request, IServerStreamWriter<GetTicksResponse> responseStream, ServerCallContext context);
}
