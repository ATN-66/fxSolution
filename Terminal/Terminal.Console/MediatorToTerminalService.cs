/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                      MediatorToTerminalService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Google.Protobuf;
using Grpc.Core;

namespace Terminal.Console;

public class MediatorToTerminalService : MediatorToTerminal.MediatorToTerminalBase
{
    private const string Host = "localhost";
    private const int Port = 8080;

    public Task StartAsync()
    {
        var grpcServer = new Server
        {
            Services = { MediatorToTerminal.BindService(this) }, Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        System.Console.WriteLine($"MediatorToTerminalService listening on {Host}:{Port}");
        var tcs = new TaskCompletionSource<bool>();
        return tcs.Task;
    }

    public override async Task TickAsync(IAsyncStreamReader<grpcquotation> requestStream, IServerStreamWriter<Reply> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
        {
            var q = requestStream.Current;
            var quotation = new Quotation(q.Id, (Symbol)q.Symbol, q.DateTime.ToDateTime(), q.Doubleask, q.Doublebid, q.Intask, q.Intbid);
            //if (quotation.Symbol == Symbol.EURUSD) System.Console.WriteLine(quotation);
            System.Console.WriteLine(quotation);
            var message = new Reply { ReplyMessage = "ok" };
            await responseStream.WriteAsync(message).ConfigureAwait(false);
        }
    }
}