using Grpc.Net.Client;

namespace Terminal.WinUI3.Contracts.Services;

public interface IExecutiveConsumerService
{
    Task<(Task, GrpcChannel)> StartAsync(CancellationToken token);
}
