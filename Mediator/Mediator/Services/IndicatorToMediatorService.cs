/*+------------------------------------------------------------------+
  |                                                Mediator.Services |
  |                                    IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using System.Reflection;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;
using Mediator.Services.PipeMethodCalls;
using Microsoft.Extensions.Logging;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using Symbol = Common.Entities.Symbol;

namespace Mediator.Services;

internal class IndicatorToMediatorService : IIndicatorToMediatorService
{
    private readonly Guid _guid = Guid.NewGuid();
    private string _pipeName = "Indicator.To.Mediator";
    private readonly IDataProviderService _dataProviderService;
    private readonly ILogger<IndicatorToMediatorService> _logger;
    private readonly IPipeSerializer _pipeSerializer = new NetJsonPipeSerializer();

    public IndicatorToMediatorService(IDataProviderService dataProviderService, ILogger<IndicatorToMediatorService> logger) 
    {
        _dataProviderService = dataProviderService;
        _logger = logger;
    }

    public async Task StartAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        _pipeName = $"{_pipeName}.{(int)symbol}";
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                PipeServer<ITicksMessenger> pipeServer;
                using (pipeServer = new PipeServer<ITicksMessenger>(_pipeSerializer, _pipeName, () => new IndicatorToMediatorMessenger(_dataProviderService)))
                {
                    _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is ON.", _pipeName, _guid);
                    await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await pipeServer.WaitForRemotePipeCloseAsync(cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                pipeServer.Dispose();
            }
            catch (OperationCanceledException)
            {

                break;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, $"pipeName:({_pipeName}).({_guid})");
                break;
            }
            finally
            {
                await _dataProviderService.DeInitAsync((int)DeInitReason.Terminal_closed).ConfigureAwait(false);
                _logger.Log(LogLevel.Trace, $"{_pipeName}.({_guid}) is OFF.");
            }
        }
    }
}