﻿/*+------------------------------------------------------------------+
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

    public async Task StartAsync(Symbol symbol, CancellationToken token)
    {
        _pipeName = $"{_pipeName}.{(int)symbol}";
        while (!token.IsCancellationRequested)
        {
            try
            {
                PipeServer<ITicksMessenger> pipeServer;
                using (pipeServer = new PipeServer<ITicksMessenger>(_pipeSerializer, _pipeName, () => new IndicatorToMediatorMessenger(_dataProviderService)))
                {
                    _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is ON.", _pipeName, _guid.ToString());
                    await pipeServer.WaitForConnectionAsync(token).ConfigureAwait(false);
                    await pipeServer.WaitForRemotePipeCloseAsync(token).ConfigureAwait(false);
                }

                pipeServer.Dispose();
            }
            catch (OperationCanceledException)
            {
               break;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, $"pipeName:({_pipeName}).({_guid})");
                throw;
            }
            finally
            {
                switch (token.IsCancellationRequested)
                {
                    case false:
                        await _dataProviderService.DeInitAsync((int)DeInitReason.Terminal_closed).ConfigureAwait(false);
                        _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is OFF.", _pipeName, _guid.ToString());
                        break;
                }
            }
        }
    }
}