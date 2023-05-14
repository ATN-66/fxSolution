/*+------------------------------------------------------------------+
  |                             Mediator.Client.Mediator.To.Terminal |
  |                                      MediatorToTerminalClient.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Diagnostics;
using Common.Entities;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Channel = Grpc.Core.Channel;

namespace Mediator.Client.Mediator.To.Terminal;

public sealed class MediatorToTerminalClient : IDisposable
{
    private const string Host = "localhost";
    private const int Port = 8080;
    private readonly Channel channel;
    private AsyncDuplexStreamingCall<grpcquotation, Reply>? call;
    private readonly MediatorToTerminal.MediatorToTerminalClient client;
    private readonly BlockingCollection<Quotation> quotations = new();

    public MediatorToTerminalClient()
    {
        channel = new Channel(Host + ":" + Port, ChannelCredentials.Insecure);
        client = new MediatorToTerminal.MediatorToTerminalClient(channel);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        call = client.TickAsync(cancellationToken: ct);

        while (await call.ResponseStream.MoveNext(ct).ConfigureAwait(false))
        {
            var serverMessage = call.ResponseStream.Current;
            var otherClientMessage = serverMessage.ReplyMessage;
            Debug.Assert(otherClientMessage == "ok");
        }
    }

    public async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var quotation in quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            var message = new grpcquotation()
            {
                Id = quotation.ID,
                Symbol = (int)quotation.Symbol,
                DateTime = Timestamp.FromDateTime(quotation.DateTime),
                Doubleask = quotation.DoubleAsk,
                Doublebid = quotation.DoubleBid,
                Intask = quotation.IntAsk,
                Intbid = quotation.IntBid
            };

            await call!.RequestStream.WriteAsync(message, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public void Tick(Quotation quotation)
    {
        quotations.Add(quotation);
    }

    public async void Dispose()
    {
        call?.Dispose();
        await channel.ShutdownAsync().ConfigureAwait(false);
        quotations.Dispose();
    }
}