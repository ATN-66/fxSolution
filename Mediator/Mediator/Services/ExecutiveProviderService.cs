using Common.Entities;
using Common.ExtensionsAndHelpers;
using Fx.Grpc;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mediator.Services;

public class ExecutiveProviderService : ExecutiveProvider.ExecutiveProviderBase, IExecutiveProviderService
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly MainViewModel _mainViewModel;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<IDataProviderService> _logger;
    private readonly ExecutiveProviderSettings _executiveProviderSettings;

    private const int RetryCountLimit = 5;
    private const int Backoff = 5;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private static volatile bool _isFaulted;

    private ServiceStatus _serviceStatus;
    private ClientStatus _clientStatus;

    public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged = null!;
    public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged = null!;

    public ExecutiveProviderService(IConfiguration configuration, IOptions<ExecutiveProviderSettings> executiveProviderSettings, MainViewModel mainViewModel, CancellationTokenSource cts, ILogger<IDataProviderService> logger)
    {
        _mainViewModel = mainViewModel;
        ServiceStatusChanged += (_, e) => { _mainViewModel.ExecutiveProviderServiceStatus = e.ServiceStatus; };
        ClientStatusChanged += (_, e) => { _mainViewModel.ExecutiveProviderClientStatus = e.ClientStatus; };
        _cts = cts;
        _logger = logger;

        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
        _executiveProviderSettings = executiveProviderSettings.Value;

        _serviceStatus = ServiceStatus.Off;
        _clientStatus = ClientStatus.Off;

        _logger.LogTrace("{_guid} is ON. {_executiveProviderHost}:{_executiveProviderPort}", _guid, _executiveProviderSettings.Host, _executiveProviderSettings.Port);
    }
    
    private ServiceStatus ServiceStatus
    {
        get => _serviceStatus;
        set
        {
            if (value == _serviceStatus)
            {
                return;
            }

            _serviceStatus = value;
            ServiceStatusChanged(this, new ServiceStatusChangedEventArgs(ServiceStatus));
        }
    }
    private ClientStatus ClientStatus
    {
        get => _clientStatus;
        set
        {
            if (value == _clientStatus)
            {
                return;
            }

            _clientStatus = value;
            ClientStatusChanged(this, new ClientStatusChangedEventArgs(ClientStatus));
        }
    }

    public async Task StartAsync()
    {
        var retryCount = 0;

        while (retryCount < RetryCountLimit && !_cts.Token.IsCancellationRequested)
        {
            Server? grpcServer = null;
            _isFaulted = false;

            try
            {
                grpcServer = new Server(new List<ChannelOption>
                {
                    new("grpc.max_send_message_length", _maxSendMessageSize),
                    new("grpc.max_receive_message_length", _maxReceiveMessageSize)

                })
                {
                    Services = { ExecutiveProvider.BindService(this) },
                    Ports = { new ServerPort(_executiveProviderSettings.Host, _executiveProviderSettings.Port, ServerCredentials.Insecure) },
                };
                grpcServer.Start();

                ServiceStatus = ServiceStatus.On;
                _logger.LogTrace("Listening on {host}:{port}", _executiveProviderSettings.Host, _executiveProviderSettings.Port.ToString());
                await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);

                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled, shutting down executive provider gRPC server...");
                break;  // Break out of the loop on cancellation, assuming we don't want to retry in this case
            }
            catch (IOException ioException)
            {
                LogExceptionHelper.LogException(_logger, ioException, "");
                retryCount++;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, "");
                retryCount++;
            }
            finally
            {
                if (grpcServer != null)
                {
                    ServiceStatus = ServiceStatus.Off;
                    await grpcServer.ShutdownAsync().ConfigureAwait(false);
                    _logger.LogTrace("executive provider gRPC Server shutted down.");
                }
            }

            if (retryCount > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Backoff * retryCount)).ConfigureAwait(false);
            }
        }

        if (retryCount >= RetryCountLimit)
        {
            _mainViewModel.AtFault = true;
            _logger.LogCritical("CRITICAL ERROR: The executive provider gRPC server has repeatedly failed to start after {retryCount} attempts. This indicates a severe underlying issue that needs immediate attention. The server will not try to restart again. Please check the error logs for more information and take necessary action immediately.", retryCount);
        }
    }

    public async override Task CommunicateAsync(IAsyncStreamReader<GeneralRequest> requestStream, IServerStreamWriter<GeneralResponse> responseStream, ServerCallContext context)
    {
        if (ServiceStatus != ServiceStatus.On)
        {
            return;
        }

        ClientStatus = ClientStatus.On;

        try
        {
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                var message = requestStream.Current;
                // Handle the received message...
            }
        }
        catch (Exception exception)
        {
            var st0 = exception;
            if (context.CancellationToken.IsCancellationRequested)
            {
                var st1 = true;
            }
            throw;
        }

        ClientStatus = ClientStatus.Off;
    }
}