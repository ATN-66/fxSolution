using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fx.Grpc;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

internal class ExecutiveConsumerService : IExecutiveConsumerService
{
    private const int Deadline = int.MaxValue;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private readonly string _grpcExecutiveChannelAddress;

    public ExecutiveConsumerService(IConfiguration configuration, ILogger<IDataConsumerService> logger) 
    {
        _grpcExecutiveChannelAddress = configuration.GetValue<string>($"{nameof(_grpcExecutiveChannelAddress)}")!;
        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
    }

    public async Task<(Task, GrpcChannel)> StartAsync(CancellationToken token)
    {
        var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = _maxSendMessageSize, MaxReceiveMessageSize = _maxReceiveMessageSize };
        var channel = GrpcChannel.ForAddress(_grpcExecutiveChannelAddress, channelOptions);
        var client = new ExecutiveProvider.ExecutiveProviderClient(channel);
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(Deadline)));
        var request = new GeneralRequest
        {
           Type = MessageType.MaintenanceCommand,
           MaintenanceRequest = new MaintenanceRequest { Code = MaintenanceRequest.Types.Code.OpenSession }
        };

        var call = client.CommunicateAsync(callOptions);
        await call.RequestStream.WriteAsync(request, token).ConfigureAwait(false);

        var executiveTask = Task.Run(async () =>
        {
            try
            {
                int counter = default;
                await foreach (var response in call.ResponseStream.ReadAllAsync(token).WithCancellation(token))
                {
                    switch (response.Type)
                    {
                        case MessageType.MaintenanceCommand:

                            break;
                        case MessageType.AccountInfo:
                            break;
                        case MessageType.TradeCommand:
                            break;
                        case MessageType.TradeInfo:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                if (token.IsCancellationRequested)
                {
                    throw;
                }
                else
                {
                    throw;
                }
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode == StatusCode.Cancelled)
                {
                    //ignore
                }
                else
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                throw;
            }
            finally
            {
            }
        }, token);

        return (executiveTask, channel);
    }
}