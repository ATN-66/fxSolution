using Common.Entities;
using Fx.Grpc;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mediator.Services;

public class ExecutiveConsumerService : IExecutiveConsumerService
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly MainViewModel _mainViewModel;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<IDataProviderService> _logger;
    private readonly ExecutiveConsumerSettings _executiveConsumerSettings;

    private const int RetryCountLimit = 5;
    private const int Backoff = 5;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private static volatile bool _isFaulted;

    private ServiceStatus _serviceStatus;
    private ClientStatus _clientStatus;

    public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged = null!;
    public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged = null!;

    public ExecutiveConsumerService(IConfiguration configuration, IOptions<ExecutiveConsumerSettings> executiveConsumerSettings, MainViewModel mainViewModel, CancellationTokenSource cts, ILogger<IDataProviderService> logger)
    {
        _mainViewModel = mainViewModel;
        ServiceStatusChanged += (_, e) => { _mainViewModel.ExecutiveSupplierServiceStatus = e.ServiceStatus; };
        ClientStatusChanged += (_, e) => { _mainViewModel.ExecutiveSupplierClientStatus = e.ClientStatus; };
        _cts = cts;
        _logger = logger;

        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
        _executiveConsumerSettings = executiveConsumerSettings.Value;

        _serviceStatus = ServiceStatus.Off;
        _clientStatus = ClientStatus.Off;

        _logger.LogTrace("{_guid} is ON. {_executiveProviderHost}:{_executiveProviderPort}", _guid, _executiveConsumerSettings.Host, _executiveConsumerSettings.Port);
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

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public Task CommunicateAsync(IAsyncStreamReader<GeneralRequest> requestStream, IServerStreamWriter<GeneralResponse> responseStream, ServerCallContext context)
    {
       return Task.CompletedTask;
    }
}