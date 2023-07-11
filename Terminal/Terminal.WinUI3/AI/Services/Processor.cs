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
using Terminal.WinUI3.Services;

namespace Terminal.WinUI3.AI.Services;

public class Processor : IProcessor
{
    private readonly IDataService _dataService;
    private readonly IExecutiveConsumerService _executiveConsumerService;
    private readonly IVisualService _visualService;
    private readonly ISplashScreenService _splashScreenService;
    private readonly ILogger<IProcessor> _logger;

    private readonly BlockingCollection<Quotation> _liveDataQueue = new();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    private readonly DateTime _startDateTime;

    public Processor(IDataService dataService, IExecutiveConsumerService executiveConsumerService, IVisualService visualService, ISplashScreenService splashScreenService, ILogger<IProcessor> logger)
    {
        _dataService = dataService;
        _executiveConsumerService = executiveConsumerService;
        _visualService = visualService;
        _splashScreenService = splashScreenService;
        _logger = logger;

        //_startDateTime = DateTime.Now.AddDays(0).AddHours(0);
        _startDateTime = new DateTime(2023, 02, 19, 10, 0, 0);
    }

    public async Task StartAsync(CancellationToken token)
    {
        _splashScreenService.DisplaySplash();
        _kernels = await _dataService.LoadDataAsync(_startDateTime).ConfigureAwait(true);
        _visualService.Initialize(_kernels);
        _splashScreenService.HideSplash();

        var (dataTask, dataChannel) = await _dataService.StartAsync(_liveDataQueue, token).ConfigureAwait(false);
        var dataProcessingTask = DataProcessingTaskAsync(token);

        var (executiveTask, executiveChannel) = await _executiveConsumerService.StartAsync(token).ConfigureAwait(false);
        //var processingTask = ExecutiveProcessingTaskAsync(token);

        //await Task.WhenAll(dataTask, dataProcessingTask).ConfigureAwait(false);
        await Task.WhenAll(dataTask, dataProcessingTask, executiveTask).ConfigureAwait(false);
        //await Task.WhenAll(executiveTask).ConfigureAwait(false);

        await dataChannel.ShutdownAsync().ConfigureAwait(false);
        await executiveChannel.ShutdownAsync().ConfigureAwait(false);
    }
    private Task DataProcessingTaskAsync(CancellationToken token)
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