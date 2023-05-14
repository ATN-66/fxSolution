/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                      MediatorToTerminalService.cs |
  +------------------------------------------------------------------+*/

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
            var gQuotation = requestStream.Current;
            //var quotation = new Quotation(gQuotation.Id, (Symbol)gQuotation.Symbol, gQuotation.DateTime.ToDateTime(), gQuotation.Doubleask, gQuotation.Doublebid, gQuotation.Intask, gQuotation.Intbid);
            //if (quotation.Symbol == Symbol.EURUSD) System.Console.WriteLine(quotation);

            System.Console.WriteLine(gQuotation.DateTime);
            var message = new Reply { ReplyMessage = "ok" };
            await responseStream.WriteAsync(message).ConfigureAwait(false);
        }
    }
}