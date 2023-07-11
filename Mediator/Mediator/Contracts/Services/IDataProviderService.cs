/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                          IDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Fx.Grpc;
using Grpc.Core;

namespace Mediator.Contracts.Services;

public interface IDataProviderService
{
    Task DeInitAsync(int symbol, int reason);
    Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace);
    string Tick(int id, int symbol, string datetime, double ask, double bid);

    Task StartAsync();
    Task GetDataAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context);
}