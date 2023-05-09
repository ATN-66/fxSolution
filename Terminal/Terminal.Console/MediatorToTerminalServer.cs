/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                      MediatorToTerminalServer.cs |
  +------------------------------------------------------------------+*/

using System.Security.Cryptography;
using System.Threading.Tasks;
using Common.Entities;
using Grpc.Core;
using Protos.Grpc;

namespace Terminal.Console;

public class MediatorToTerminalServer : MediatorToTerminalService.MediatorToTerminalServiceBase
{
    private const string Host = "localhost";
    private const int Port = 8080;

    public async Task StartAsync()
    {
        var grpcServer = new Server
        {
            Services = { MediatorToTerminalService.BindService(this) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        System.Console.WriteLine($"MediatorToTerminalServer listening on {Host}:{Port}");
        var tcs = new TaskCompletionSource<bool>();
        await tcs.Task;
    }

    public override async Task Tick(IAsyncStreamReader<gQuotation> requestStream, IServerStreamWriter<Reply> responseStream, ServerCallContext context)
    {
        throw new NotImplementedException();
        //while (await requestStream.MoveNext(CancellationToken.None))
        //{
        //    var gQuotation = requestStream.Current;
        //    var quotation = new Quotation((Symbol)gQuotation.Symbol, gQuotation.DateTime.ToDateTime(), gQuotation.Ask, gQuotation.Bid);

        //    if(quotation.Symbol == Symbol.EURUSD) System.Console.WriteLine(quotation);

        //    var message = new Reply { ReplyMessage = "ok" };
        //    await responseStream.WriteAsync(message);
        //}
    }
}