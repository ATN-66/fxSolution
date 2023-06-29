/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                          IDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Mediator.Models;
using Ticksdata;
using Quotation = Common.Entities.Quotation;

namespace Mediator.Contracts.Services;

public interface IDataProviderService
{
    event EventHandler<ThreeStateChangedEventArgs> IsServiceActivatedChanged;
    event EventHandler<TwoStateChangedEventArgs> IsClientActivatedChanged;
    Task StartAsync();
    Task SaveQuotationsAsync(IEnumerable<Quotation> quotations);
    Task GetSinceDateTimeHourTillNowAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context);
}
