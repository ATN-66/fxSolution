using Fx.Grpc;
using Grpc.Core;

namespace Mediator.Contracts.Services;

public interface IExecutiveConsumerService
{
    Task StartAsync();
    Task CommunicateAsync(IAsyncStreamReader<GeneralRequest> requestStream, IServerStreamWriter<GeneralResponse> responseStream, ServerCallContext context);
}