/*+------------------------------------------------------------------+
  |                                                Mediator.Services |
  |                                           DataConsumerService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;
using Mediator.Models;
using Microsoft.Extensions.Logging;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using Symbol = Common.Entities.Symbol;

namespace Mediator.Services;

internal class DataConsumerService : IDataConsumerService
{
    private readonly Guid _guid = Guid.NewGuid();
    private Symbol _symbol;
    private string _pipeName = "DATA";
    private readonly IDataProviderService _dataProviderService;
    private readonly ILogger<IDataConsumerService> _logger;
    private readonly IPipeSerializer _pipeSerializer = new NetJsonPipeSerializer();

    private static IDataMessenger? _dataMessenger;

    public DataConsumerService(IDataProviderService dataProviderService, ILogger<IDataConsumerService> logger) 
    {
        _dataProviderService = dataProviderService;
        _dataMessenger = null!;

        _logger = logger;
    }

    public async Task StartAsync(Symbol symbol, CancellationToken token)
    {
        _symbol = symbol;
        _pipeName = $"{_pipeName}.{(int)_symbol}";
        while (!token.IsCancellationRequested)
        {
            try
            {
                PipeServer<IDataMessenger?> pipeServer;
                using (pipeServer = new PipeServer<IDataMessenger?>(_pipeSerializer, _pipeName, () => _dataMessenger ??= new DataMessenger(_dataProviderService)))
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
                        await _dataProviderService.DeInitAsync((int)_symbol, (int)DeInitReason.Terminal_closed).ConfigureAwait(false);
                        _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is OFF.", _pipeName, _guid.ToString());
                        break;
                }
            }
        }
    }
}