/*+------------------------------------------------------------------+
  |                                      Terminal.WinUI3.AI.Services |
  |                                                     Processor.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.AI.Services;

public class Processor : IProcessor
{
    private readonly IDataService _dataService;
    private readonly IVisualService _visualService;
    private readonly ILogger<IProcessor> _logger;

    private readonly BlockingCollection<Quotation> _liveDataQueue = new();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    private readonly DateTime _startDateTime;

    public Processor(IDataService dataService, IVisualService visualService, ILogger<IProcessor> logger)
    {
        _dataService = dataService;
        _visualService = visualService;
        _logger = logger;

        _startDateTime = DateTime.Now.AddDays(-3).AddHours(1);
    }

    public async Task StartAsync(CancellationToken token)
    {
        _kernels = await _dataService.LoadDataAsync(_startDateTime).ConfigureAwait(false);
        _visualService.Initialize(_kernels);

        var (receivingTask, channel) = await _dataService.StartAsync(_liveDataQueue, token).ConfigureAwait(false);
        var processingTask = ProcessingTaskAsync(token);

        await Task.WhenAll(receivingTask, processingTask).ConfigureAwait(false);
        await channel.ShutdownAsync().ConfigureAwait(false);
    }
    private Task ProcessingTaskAsync(CancellationToken token)
    {
        var processingTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var quotation in _liveDataQueue.GetConsumingAsyncEnumerable(token).WithCancellation(token))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    _kernels[quotation.Symbol].Add(quotation);
                    _visualService.Tick(quotation.Symbol);
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                LogExceptionHelper.LogException(_logger, operationCanceledException, "");
                throw;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, "");
                throw;
            }
        }, token);

        return processingTask;
    }
}