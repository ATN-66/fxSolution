/*+------------------------------------------------------------------+
  |                             Mediator.Server.Terminal.To.Mediator |
  |                                      TerminalToMediatorServer.cs |
  +------------------------------------------------------------------+*/

using Grpc.Core;
using Mediator.Processors;
using Protos.Grpc;

namespace Mediator.Server.Terminal.To.Mediator;

public class TerminalToMediatorServer : TerminalToMediatorService.TerminalToMediatorServiceBase
{
    private const string Host = "localhost";
    private const int Port = 50051;
    private readonly OrdersProcessor _ordersProcessor;

    public TerminalToMediatorServer(OrdersProcessor ordersProcessor)
    {
        _ordersProcessor = ordersProcessor;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var grpcServer = new Grpc.Core.Server
        {
            Services = { TerminalToMediatorService.BindService(this) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        Console.WriteLine($"TerminalToMediatorServer listening on {Host}:{Port}");
        cancellationToken.Register(async () =>
        {
            await grpcServer.ShutdownAsync();
            Console.WriteLine("TerminalToMediatorServer stopped");
        });
        await Task.Delay(-1, cancellationToken);
    }

    public override async Task<Response> DeInit(Request request, ServerCallContext context)
    {
        return await _ordersProcessor.DeInitAsync(request);
    }

    public override async Task<Response> Init(Request request, ServerCallContext context)
    {
        try
        {
            return await _ordersProcessor.InitAsync(request);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}