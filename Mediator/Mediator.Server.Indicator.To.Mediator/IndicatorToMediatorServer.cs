/*+------------------------------------------------------------------+
  |                            Mediator.Server.Indicator.To.Mediator |
  |                                     IndicatorToMediatorServer.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Diagnostics;
using Common.Entities;
using Common.MetaQuotes.Mediator;
using Mediator.Processors;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace Mediator.Server.Indicator.To.Mediator;

public class IndicatorToMediatorServer : IIndicatorToMediatorServer
{
    private readonly QuotationsProcessor _quotationsProcessor;
    private readonly IPipeSerializer pipeSerializer = new NetJsonPipeSerializer();

    public IndicatorToMediatorServer(QuotationsProcessor quotationsProcessor)
    {
        _quotationsProcessor = quotationsProcessor;
    }

    public async Task StartAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var PipeName = $"IndicatorToMediator_{symbol}";

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
                Console.WriteLine($"Server error: {ex.Message}");
                break;
            }
    }
}