/*+------------------------------------------------------------------+
  |                            Mediator.Service.Terminal.To.Mediator |
  |                                     TerminalToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Mediator.Processors;
using Protos.Grpc;

namespace Mediator.Service.Terminal.To.Mediator;

public class TerminalToMediatorService : TerminalToMediator.TerminalToMediatorBase
{
    private const string Host = "localhost";
    private const int Port = 50051;
    private readonly OrdersProcessor _ordersProcessor;

    public TerminalToMediatorService(OrdersProcessor ordersProcessor)
    {
        _ordersProcessor = ordersProcessor;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var grpcServer = new Grpc.Core.Server
        {
            Services = { TerminalToMediator.BindService(this) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        Console.WriteLine($"TerminalToMediatorService listening on {Host}:{Port}");
        cancellationToken.Register(async () =>
        {
            await grpcServer.ShutdownAsync().ConfigureAwait(true);
            Console.WriteLine("TerminalToMediatorService stopped");
        });
        await Task.Delay(-1, cancellationToken).ConfigureAwait(true);
    }

    public async Task<Response> DeInitAsync(Request request, ServerCallContext context)
    {
        return await _ordersProcessor.DeInitAsync(request).ConfigureAwait(false);
    }

    public async Task<Response> InitAsync(Request request, ServerCallContext context)
    {
        try
        {
            return await _ordersProcessor.InitAsync(request).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
}