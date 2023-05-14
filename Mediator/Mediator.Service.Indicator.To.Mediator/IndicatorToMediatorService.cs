/*+------------------------------------------------------------------+
  |                            Mediator.Server.Indicator.To.Mediator |
  |                                     IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Common.MetaQuotes.Mediator;
using Mediator.Processors;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace Mediator.Service.Indicator.To.Mediator;

public class IndicatorToMediatorService : IIndicatorToMediatorService
{
    private readonly QuotationsProcessor _quotationsProcessor;
    private readonly IPipeSerializer pipeSerializer = new NetJsonPipeSerializer();

    public IndicatorToMediatorService(QuotationsProcessor quotationsProcessor)
    {
        _quotationsProcessor = quotationsProcessor;
    }

    public async Task StartAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var PipeName = $"IndicatorToMediator_{(int)symbol}";

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                using var pipeServer = new PipeServer<IQuotationsMessenger>(pipeSerializer, PipeName,() => new QuotationsMessenger(_quotationsProcessor));
                if (cancellationToken.IsCancellationRequested) break;
                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested) break;
                await pipeServer.WaitForRemotePipeCloseAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Operation cancellation is a part of the expected program flow and does not require additional handling.
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Console.WriteLine($"{GetType().Name}: {ex.Message}");
                break;
            }
    }
}