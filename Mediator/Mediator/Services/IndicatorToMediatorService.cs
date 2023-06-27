/*+------------------------------------------------------------------+
  |                                                Mediator.Services |
  |                                    IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;
using Mediator.Helpers;
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
    private readonly ITicksProcessor _ticksProcessor;
    private readonly ILogger<IndicatorToMediatorService> _logger;
    private readonly IPipeSerializer _pipeSerializer = new NetJsonPipeSerializer();

    public IndicatorToMediatorService(ITicksProcessor ticksProcessor, ILogger<IndicatorToMediatorService> logger)
    {
        _ticksProcessor = ticksProcessor;
        _logger = logger;
    }

    public async Task StartAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        _pipeName = $"{_pipeName}.{(int)symbol}";
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new PipeServer<ITicksMessenger>(_pipeSerializer, _pipeName, () => new IndicatorToMediatorMessenger(_ticksProcessor));
                _logger.LogTrace("pipeName:({_pipeName}).({_guid}) is ON.", _pipeName, _guid);
                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                await pipeServer.WaitForRemotePipeCloseAsync(cancellationToken).ConfigureAwait(false);
                await _ticksProcessor.DeInitAsync((int)DeInitReason.Terminal_closed).ConfigureAwait(false);
                _logger.Log(LogLevel.Trace, $"{_pipeName}.({_guid}) is OFF.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, $"pipeName:({_pipeName}).({_guid})");
                break;
            }
        }
    }
}