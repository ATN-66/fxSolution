/*+------------------------------------------------------------------+
  |                             Mediator.Client.Mediator.To.Terminal |
  |                                      MediatorToTerminalClient.cs |
  +------------------------------------------------------------------+*/

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
    private MediatorToTerminalService.MediatorToTerminalServiceClient _mediatorToTerminalService;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        //var channel = new Channel(Host + ":" + Port, ChannelCredentials.Insecure);
        //_mediatorToTerminalService = new MediatorToTerminalService.MediatorToTerminalServiceClient(channel);

        //try
        //{
        //    using (_call = _mediatorToTerminalService.Tick(cancellationToken: cancellationToken))
        //    {
        //        while (await _call.ResponseStream.MoveNext(CancellationToken.None))
        //        {
        //            var serverMessage = _call.ResponseStream.Current;
        //            var otherClientMessage = serverMessage.ReplyMessage;
        //            //var displayMessage = string.Format("{0}:{1}{2}", otherClientMessage.From, otherClientMessage.Message, Environment.NewLine);
        //            //chatTextBox.Text += displayMessage;
        //        }
        //    }
        //}
        //catch (RpcException exception)//TODO:
        //{
        //    throw;
        //}

        //Console.WriteLine($"MediatorToTerminalClient listening on {Host}:{Port}");
        //await Task.Delay(-1, cancellationToken);
    }

    public async void Tick(Quotation quotation)
    {
        throw new NotImplementedException();
        //if (_administrator.TerminalIsON)
        //    if (_call == null) return;

        //var message = new gQuotation
        //{
        //    Symbol = (int)quotation.Symbol,
        //    DateTime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()), //should be UTC already
        //    Broker = (int)quotation.Broker,
        //    Ask = quotation.Ask,
        //    Bid = quotation.Bid
        //};

        //await _call.RequestStream.WriteAsync(message);
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