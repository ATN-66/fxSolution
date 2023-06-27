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
    event EventHandler<ActivationChangedEventArgs> IsServiceActivatedChanged;
    event EventHandler<ActivationChangedEventArgs> IsClientActivatedChanged;
    bool IsServiceActivated { get; set; }
    Task StartAsync();
    Task GetSinceDateTimeHourTillNowAsync(DataRequest request, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context);
}
