/*+------------------------------------------------------------------+
  |                             Mediator.Client.Mediator.To.Terminal |
  |                                      MediatorToTerminalClient.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Protos.Grpc;

namespace Mediator.Client.Mediator.To.Terminal;

public class MediatorToTerminalClient : IDisposable
{
    const string Host = "localhost";
    const int Port = 8080;

    private AsyncDuplexStreamingCall<gQuotation, Reply>? _call;
    private MediatorToTerminal.MediatorToTerminalClient _client;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var channel = new Channel(Host + ":" + Port, ChannelCredentials.Insecure);
        _client = new MediatorToTerminal.MediatorToTerminalClient(channel);
        
        using (_call = _client.TickAsync(cancellationToken: cancellationToken))
        {
            while (await _call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
            {
                var serverMessage = _call.ResponseStream.Current;
                var otherClientMessage = serverMessage.ReplyMessage;
                Debug.Assert(otherClientMessage == "ok");
            }
        }

        Console.WriteLine($"{GetType().Name} is listening on {Host}:{Port}");
        await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
    }

    public async void Tick(Quotation quotation)
    {
        if (_call is null) return;

        var message = new gQuotation
        {
            Symbol = (int)quotation.Symbol,
            DateTime = Timestamp.FromDateTime(quotation.DateTime),
            Ask = quotation.IntAsk,
            Bid = quotation.IntBid
        };

        await _call.RequestStream.WriteAsync(message).ConfigureAwait(false);
    }

    ~MediatorToTerminalClient()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            //Free any managed objects here if needed
        }

        //Free any unmanaged objects here if needed
    }
}