/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                      TerminalToMediatorClient.cs |
  +------------------------------------------------------------------+*/

using Grpc.Net.Client;
using Protos.Grpc;

namespace Terminal.Console;

public class TerminalToMediatorClient : IDisposable
{
    private const string address = "http://localhost:50051";
    private readonly TerminalToMediator.TerminalToMediatorClient _client;
    private readonly GrpcChannel channel;

    public TerminalToMediatorClient()
    {
        channel = GrpcChannel.ForAddress(address);
        _client = new TerminalToMediator.TerminalToMediatorClient(channel);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<Response> DeInitAsync(Request request)
    {
        try
        {
            var callOptions = new Grpc.Core.CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(5)));
            var response = await _client.DeInitAsync(request, callOptions).ConfigureAwait(true);
            return response;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode is Grpc.Core.StatusCode.Unavailable or Grpc.Core.StatusCode.DeadlineExceeded)
        {
            return new Response()
            {
                ResponseMessage = "Goodbye",
                ReasonMessage = ex.StatusCode.ToString()
            };
        }
    }

    public async Task<Response> InitAsync(Request request)
    {
        try
        {
            var callOptions = new Grpc.Core.CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(5)));
            return await _client.InitAsync(request, callOptions);
        }
        catch (Exception e)
        {
            var st = true;
            throw;
        }
        //catch (Grpc.Core.RpcException ex) when (ex.StatusCode is Grpc.Core.StatusCode.Unavailable or Grpc.Core.StatusCode.DeadlineExceeded)
        //{
        //    return new Response()
        //    {
        //        ResponseMessage = "Goodbye",
        //        ReasonMessage = ex.StatusCode.ToString()
        //    };
        //}

    }

    ~TerminalToMediatorClient()
    {
        Dispose(true);
    }

    private async void Dispose(bool disposing)
    {
        if (disposing) await channel.ShutdownAsync().ConfigureAwait(false);
        //Free any unmanaged objects here if needed
    }
}