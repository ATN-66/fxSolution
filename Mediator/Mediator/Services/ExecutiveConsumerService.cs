/*+------------------------------------------------------------------+
  |                                                Mediator.Services |
  |                                      ExecutiveConsumerService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.ViewModels;
using Microsoft.Extensions.Logging;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace Mediator.Services;

public class ExecutiveConsumerService : IExecutiveConsumerService
{
    private readonly Guid _guid = Guid.NewGuid();
    private const string PipeName = "EXECUTIVE";
    private readonly IExecutiveProviderService _executiveProviderService;
    private readonly ILogger<IExecutiveConsumerService> _logger;
    private readonly IPipeSerializer _pipeSerializer = new NetJsonPipeSerializer();

    private static IExecutiveMessenger? _executiveMessenger;

    private ServiceStatus _serviceStatus;
    private ClientStatus _clientStatus;

    public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged = null!;
    public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged = null!;

    public ExecutiveConsumerService(IExecutiveProviderService executiveProviderService, MainViewModel mainViewModel, ILogger<IExecutiveConsumerService> logger)
    {
        var mainViewModel1 = mainViewModel;
        ServiceStatusChanged += (_, e) => { mainViewModel1.ExecutiveSupplierServiceStatus = e.ServiceStatus; };
        ClientStatusChanged += (_, e) => { mainViewModel1.ExecutiveSupplierClientStatus = e.ClientStatus; };
        _serviceStatus = ServiceStatus.Off;
        _clientStatus = ClientStatus.Off;

        _executiveProviderService = executiveProviderService;
        _executiveMessenger = null!;

        _logger = logger;
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

    public async Task StartAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                PipeServer<IExecutiveMessenger?> pipeServer;
                using (pipeServer = new PipeServer<IExecutiveMessenger?>(_pipeSerializer, PipeName, () => _executiveMessenger ??= new ExecutiveMessenger(_executiveProviderService)))
                {
                    ServiceStatus = ServiceStatus.On;
                    _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is ON.", PipeName, _guid.ToString());
                    await pipeServer.WaitForConnectionAsync(token).ConfigureAwait(false);
                    ClientStatus = ClientStatus.On;
                    await pipeServer.WaitForRemotePipeCloseAsync(token).ConfigureAwait(false);
                    ClientStatus = ClientStatus.Off;
                }

                pipeServer.Dispose();
                ServiceStatus = ServiceStatus.Off;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                ServiceStatus = ServiceStatus.Fault;
                LogExceptionHelper.LogException(_logger, exception, $"pipeName:({PipeName}).({_guid})");
                throw;
            }
            finally
            {
                _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is OFF.", PipeName, _guid.ToString());
            }
        }
    }
}