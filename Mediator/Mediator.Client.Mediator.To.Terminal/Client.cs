/*+------------------------------------------------------------------+
  |                             Mediator.Client.Mediator.To.Terminal |
  |                                                        Client.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Diagnostics;
using Common.Entities;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Administrator;
using Channel = Grpc.Core.Channel;

namespace Mediator.Client.Mediator.To.Terminal;

public sealed class Client : IDisposable
{
    private readonly Channel _channel;
    private AsyncDuplexStreamingCall<grpcquotation, Reply>? _call;
    private readonly MediatorToTerminal.MediatorToTerminalClient _client;
    private readonly BlockingCollection<Quotation> _quotations = new();
    private readonly Settings _settings;

    public Client(Settings settings)
    {
        _settings = settings;
        _channel = new Channel(_settings.ClientMediatorToTerminalHost + ":" + _settings.ClientMediatorToTerminalPort, ChannelCredentials.Insecure);
        _client = new MediatorToTerminal.MediatorToTerminalClient(_channel);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        Debug.Assert(_settings.TerminalConnected);
        _call = _client.TickAsync(cancellationToken: ct);

        try
        {
            while (await _call.ResponseStream.MoveNext(ct).ConfigureAwait(false))
            {
                if (!_settings.TerminalConnected) break;
                var serverMessage = _call.ResponseStream.Current;
                var otherClientMessage = serverMessage.ReplyMessage;
                Debug.Assert(otherClientMessage == "ok");
            }
        }
        catch (RpcException)
        {
            Debug.Assert(!_settings.TerminalConnected);
        }
    }

    public async Task ProcessAsync(CancellationToken ct)
    {
        Debug.Assert(_settings.TerminalConnected);
        await foreach (var quotation in _quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            if (!_settings.TerminalConnected) break;
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

            await _call!.RequestStream.WriteAsync(message, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public void Tick(Quotation quotation)
    {
        _quotations.Add(quotation);
    }

    public async void Dispose()
    {
        _call?.Dispose();
        await _channel.ShutdownAsync().ConfigureAwait(false);
        _quotations.Dispose();
    }
}