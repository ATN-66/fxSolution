/*+------------------------------------------------------------------+
  |                            Mediator.Service.Terminal.To.Mediator |
  |                                     TerminalToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Google.Protobuf;
using Grpc.Core;
using Mediator.Processors;

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
        var grpcServer = new Server
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

    public override async Task<Response> DeInit(Request request, ServerCallContext context)
    {
        return await _ordersProcessor.DeInitAsync(request).ConfigureAwait(false);
    }

    public override async Task<Response> Init(Request request, ServerCallContext context)
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