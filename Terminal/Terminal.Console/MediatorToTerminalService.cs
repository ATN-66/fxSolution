﻿/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                      MediatorToTerminalService.cs |
  +------------------------------------------------------------------+*/

using System.Security.Cryptography;
using System.Threading.Tasks;
using Common.Entities;
using Grpc.Core;
using Protos.Grpc;

namespace Terminal.Console;

public class MediatorToTerminalService : MediatorToTerminal.MediatorToTerminalBase
{
    private const string Host = "localhost";
    private const int Port = 8080;

    public async Task StartAsync()
    {
        var grpcServer = new Server
        {
            Services = { MediatorToTerminal.BindService(this) }, Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        System.Console.WriteLine($"MediatorToTerminalService listening on {Host}:{Port}");
        var tcs = new TaskCompletionSource<bool>();
        await tcs.Task;
    }

    public override async Task TickAsync(IAsyncStreamReader<gQuotation> requestStream, IServerStreamWriter<Reply> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
        {
            var gQuotation = requestStream.Current;
            //var quotation = new Quotation((Symbol)gQuotation.Symbol, gQuotation.DateTime.ToDateTime(), gQuotation.Ask, gQuotation.Bid);
            //if (quotation.Symbol == Symbol.EURUSD) System.Console.WriteLine(quotation);

            var message = new Reply { ReplyMessage = "ok" };
            await responseStream.WriteAsync(message);
        }
    }
}