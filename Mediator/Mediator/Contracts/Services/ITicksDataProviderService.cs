/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                     ITicksDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Ticksdata;

namespace Mediator.Contracts.Services;

public interface ITicksDataProviderService
{
    Task StartAsync();
    Task<GetTicksResponse> GetTicksAsync(GetTicksRequest request, ServerCallContext context);
}
