using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fx.Grpc;
using Terminal.WinUI3.Contracts.Services;
using System.Collections.Concurrent;
using Common.ExtensionsAndHelpers;

namespace Terminal.WinUI3.Services;

internal class ExecutiveConsumerService : IExecutiveConsumerService
{
    private readonly ILogger<ExecutiveConsumerService> _logger;

    private const int Deadline = int.MaxValue;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private readonly string _grpcExecutiveChannelAddress;

    public ExecutiveConsumerService(IConfiguration configuration, ILogger<ExecutiveConsumerService> logger) 
    {
        _logger = logger;
        _grpcExecutiveChannelAddress = configuration.GetValue<string>($"{nameof(_grpcExecutiveChannelAddress)}")!;
        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
    }

    public async Task<(Task, AsyncDuplexStreamingCall<GeneralRequest, GeneralResponse>, GrpcChannel)> StartAsync(BlockingCollection<GeneralResponse> responses, CancellationToken token)
    {
        var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = _maxSendMessageSize, MaxReceiveMessageSize = _maxReceiveMessageSize };
        var channel = GrpcChannel.ForAddress(_grpcExecutiveChannelAddress, channelOptions);
        var client = new ExecutiveProvider.ExecutiveProviderClient(channel);
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(Deadline)));
        var request = new GeneralRequest
        {
           Type = MessageType.MaintenanceCommand,
           MaintenanceRequest = new MaintenanceRequest { MaintenanceCode = MaintenanceCode.OpenSession }
        };

        AsyncDuplexStreamingCall<GeneralRequest, GeneralResponse> call = client.CommunicateAsync(callOptions);
        var retryCount = 0;
        const int maxRetryCount = 3; // Maximum number of retries
        const int retryDelay = 50; // Retry delay in milliseconds

        while (true)
        {
            try
            {
                await call.RequestStream.WriteAsync(request, token).ConfigureAwait(false);
                break;
            }
            catch (RpcException rpcException) when (rpcException.StatusCode == StatusCode.Unavailable)
            {
                retryCount++;

                if (retryCount > maxRetryCount)
                {
                    LogExceptionHelper.LogException(_logger, rpcException, "");
                    throw;
                }

                await Task.Delay(retryDelay, token).ConfigureAwait(false);
            }
        }

        var executiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync(token).WithCancellation(token))
                {
                    responses.Add(response, token);
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                {
                    // ignore
                }
                throw;
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode is StatusCode.Cancelled or StatusCode.Unavailable)
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
                LogExceptionHelper.LogException(_logger, exception, "");
                throw;
            }
        }, token);

        return (executiveTask, call, channel);
    }
}