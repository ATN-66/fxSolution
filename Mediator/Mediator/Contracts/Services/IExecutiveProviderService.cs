using Grpc.Core;
using Provider.Grpc;

namespace Mediator.Contracts.Services;

public interface IExecutiveProviderService
{
    Task StartAsync();
    Task CommunicateAsync(IAsyncStreamReader<GeneralRequest> requestStream, IServerStreamWriter<GeneralResponse> responseStream, ServerCallContext context);
}
