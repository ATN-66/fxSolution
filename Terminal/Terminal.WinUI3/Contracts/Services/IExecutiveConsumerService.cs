using Fx.Grpc;
using System.Collections.Concurrent;
using Grpc.Net.Client;
using Grpc.Core;

namespace Terminal.WinUI3.Contracts.Services;

public interface IExecutiveConsumerService
{
    Task<(Task, AsyncDuplexStreamingCall<GeneralRequest, GeneralResponse>, GrpcChannel)> StartAsync(BlockingCollection<GeneralResponse> responses, CancellationToken token);
}