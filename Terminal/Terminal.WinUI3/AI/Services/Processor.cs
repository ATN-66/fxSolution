/*+------------------------------------------------------------------+
  |                                      Terminal.WinUI3.AI.Services |
  |                                                     Processor.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Fx.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.AI.Services;

public class Processor : IProcessor
{
    private readonly IDataService _dataService;
    private readonly IExecutiveConsumerService _executiveConsumerService;
    private readonly IAccountService _accountService;
    private readonly CancellationToken _token;
    private readonly IVisualService _visualService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ISplashScreenService _splashScreenService;
    private readonly ILogger<IProcessor> _logger;

    private readonly BlockingCollection<Quotation> _liveDataQueue = new();
    private IDictionary<Symbol, Kernel> _kernels = null!;

    private readonly BlockingCollection<GeneralResponse> _responses = new();
    private AsyncDuplexStreamingCall<GeneralRequest, GeneralResponse> _executiveCall = null!;

    private DateTime _startDateTime;
    private DateTime _nowDateTime;
    private readonly TaskCompletionSource<bool> _dateTimesReady = new();

    public Processor(IDataService dataService, IExecutiveConsumerService executiveConsumerService, IAccountService accountService, CancellationTokenSource cancellationTokenSource, IVisualService visualService, 
        IDispatcherService dispatcherService, ISplashScreenService splashScreenService, ILogger<IProcessor> logger) {

        _dataService = dataService;
        _executiveConsumerService = executiveConsumerService;
        _accountService = accountService;
        _token = cancellationTokenSource.Token;
        _visualService = visualService;
        _dispatcherService = dispatcherService;
        _splashScreenService = splashScreenService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken token)
    {
        _splashScreenService.DisplaySplash();

        try
        {
            var (executiveTask, executiveCall, executiveChannel) = await _executiveConsumerService.StartAsync(_responses, token).ConfigureAwait(false);
            _executiveCall = executiveCall;
            var executiveProcessingTask = ExecutiveProcessingTaskAsync(token);
            await RequestIntegerAccountPropertiesAsync().ConfigureAwait(false);
            await RequestDoubleAccountPropertiesAsync().ConfigureAwait(false);
            await RequestStringAccountPropertiesAsync().ConfigureAwait(false);
            await RequestMaxVolumesAsync().ConfigureAwait(false);
            await _dateTimesReady.Task.ConfigureAwait(false);

            _kernels = await _dataService.LoadDataAsync(_startDateTime, _nowDateTime).ConfigureAwait(false);
            _visualService.Initialize(_kernels);
            var (dataTask, dataChannel) = await _dataService.StartAsync(_liveDataQueue, token).ConfigureAwait(false);
            var dataProcessingTask = DataProcessingTaskAsync(token);

            await _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                _splashScreenService.HideSplash();
            }).ConfigureAwait(true);

            await Task.WhenAll(dataTask, dataProcessingTask, executiveTask, executiveProcessingTask).ConfigureAwait(false);
            await dataChannel.ShutdownAsync().ConfigureAwait(false);
            await executiveChannel.ShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }
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
    private Task ExecutiveProcessingTaskAsync(CancellationToken token)
    {
        var processingTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in _responses.GetConsumingAsyncEnumerable(token).WithCancellation(token))
                {
                    switch (response.Type)
                    {
                        case MessageType.MaintenanceCommand:
                            switch (response.MaintenanceResponse.Code)
                            {
                                case MaintenanceResponse.Types.Code.Done:
                                    _nowDateTime = response.MaintenanceResponse.Datetime.ToDateTime().ToUniversalTime(); // todo: business logic
                                    _startDateTime = _nowDateTime.AddHours(0); // todo: business logic
                                    _dateTimesReady.SetResult(true);
                                    break;
                                case MaintenanceResponse.Types.Code.Failure: Environment.Exit(0); break;
                                default: throw new ArgumentOutOfRangeException(nameof(response.MaintenanceResponse.Code),@"Processor.ExecutiveProcessingTaskAsync: The provided response.MaintenanceResponse.Code is not supported.");
                            }
                            break;
                        case MessageType.AccountInfo:
                            switch (response.AccountInfoResponse.Code)
                            {
                                case AccountInfoCode.IntegerAccountProperties:
                                case AccountInfoCode.DoubleAccountProperties:
                                case AccountInfoCode.StringAccountProperties:
                                case AccountInfoCode.MaxVolumes:
                                    _accountService.SetUpAccountInfo(response.AccountInfoResponse.Ticket, response.AccountInfoResponse.Details);
                                    break;
                                default: throw new ArgumentOutOfRangeException(nameof(response.AccountInfoResponse.Code), @"Processor.ExecutiveProcessingTaskAsync: The provided response.AccountInfoResponse.Code is not supported.");
                            }
                            break;
                        default: throw new ArgumentOutOfRangeException(nameof(response.Type), @"Processor.ExecutiveProcessingTaskAsync: The provided response.Type is not supported.");
                    }
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

    private Task RequestIntegerAccountPropertiesAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest { Code = AccountInfoCode.IntegerAccountProperties, Ticket = 1 }
        };
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    private Task RequestDoubleAccountPropertiesAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest { Code = AccountInfoCode.DoubleAccountProperties, Ticket = 2 }
        };
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    private Task RequestStringAccountPropertiesAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest { Code = AccountInfoCode.StringAccountProperties, Ticket = 3 }
        };
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    private Task RequestMaxVolumesAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest { Code = AccountInfoCode.MaxVolumes, Ticket = 4 }
        };
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    public Task DownAsync(Symbol symbol, bool isReversed)
    {


        return Task.CompletedTask;
        //var request = new GeneralRequest
        //{
        //    Type = MessageType.MaintenanceCommand,
        //    MaintenanceRequest = new MaintenanceRequest { Code = MaintenanceRequest.Types.Code.OpenSession }
        //};
        //return _executiveCall!.RequestStream.WriteAsync(request, _token);
    }
    public Task ExitAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.MaintenanceCommand,
            MaintenanceRequest = new MaintenanceRequest { Code = MaintenanceRequest.Types.Code.CloseSession }
        };

        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
}