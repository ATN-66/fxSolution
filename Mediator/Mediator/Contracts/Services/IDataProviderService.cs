/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                          IDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Provider.Grpc;
using DataRequest = Provider.Grpc.DataRequest;

namespace Mediator.Contracts.Services;

public interface IDataProviderService
{
    Task DeInitAsync(int symbol, int reason);
    Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace);
    string Tick(int id, int symbol, string datetime, double ask, double bid);

    Task StartAsync();
    Task GetDataAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context);
}