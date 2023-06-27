/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                          IDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Mediator.Models;
using Ticksdata;

namespace Mediator.Contracts.Services;

public interface IDataProviderService
{
    event EventHandler<ThreeStateChangedEventArgs> IsServiceActivatedChanged;
    event EventHandler<TwoStateChangedEventArgs> IsClientActivatedChanged;
    Task StartAsync();
    Task GetSinceDateTimeHourTillNowAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context);
}
