/*+------------------------------------------------------------------+
  |                                      Terminal.WinUI3.AI.Services |
  |                                                     Processor.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Fx.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.AI.Models;
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
    private readonly IKernelManager _kernelManager;
    private readonly IDispatcherService _dispatcherService;
    private readonly ISplashScreenService _splashScreenService;
    private readonly ILogger<IProcessor> _logger;

    private readonly BlockingCollection<Quotation> _liveDataQueue = new();

    private readonly BlockingCollection<GeneralResponse> _responses = new();
    private AsyncDuplexStreamingCall<GeneralRequest, GeneralResponse> _executiveCall = null!;

    private DateTime _startDateTime;
    private DateTime _nowDateTime;
    private readonly TimeSpan _distanceToThePast;
    private readonly TaskCompletionSource<bool> _accountReady = new();
    public event EventHandler<PositionsEventArgs> PositionsUpdated = null!;

    public Processor(IConfiguration configuration, IDataService dataService, IExecutiveConsumerService executiveConsumerService, IAccountService accountService, IKernelManager kernelManager,
        IDispatcherService dispatcherService, ISplashScreenService splashScreenService, ILogger<IProcessor> logger, CancellationTokenSource cancellationTokenSource) {

        _dataService = dataService;
        _executiveConsumerService = executiveConsumerService;
        _accountService = accountService;
        _token = cancellationTokenSource.Token;
        _kernelManager = kernelManager;
        _dispatcherService = dispatcherService;
        _splashScreenService = splashScreenService;
        _logger = logger;

        _distanceToThePast = TimeSpan.ParseExact(configuration.GetValue<string>($"{nameof(_distanceToThePast)}")!, @"dd\:hh\:mm\:ss", null);
    }

    public async Task StartAsync(CancellationToken token)
    {
        _splashScreenService.DisplaySplash();

        try
        {
            var (executiveTask, executiveCall, executiveChannel) = await _executiveConsumerService.StartAsync(_responses, token).ConfigureAwait(false);
            _executiveCall = executiveCall;
            var executiveProcessingTask = ExecutiveProcessingTaskAsync(token);
            await RequestAccountPropertiesAsync().ConfigureAwait(false);
            await _accountReady.Task.ConfigureAwait(false);

            var historicalData = await _dataService.LoadDataAsync(_startDateTime, _nowDateTime).ConfigureAwait(false);
            _kernelManager.Initialize(historicalData);

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

    public void RequestPositionsAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        var ordersHistoryRequest = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest
            {
                AccountInfoCode = AccountInfoCode.TradingHistory, Ticket = 1,
                Details = $"start>{startDateTimeInclusive.ToUniversalTime().Date:yyyy-MM-dd};end>{endDateTimeInclusive.AddDays(1).Date.ToUniversalTime():yyyy-MM-dd}"
            }
        };

        _executiveCall.RequestStream.WriteAsync(ordersHistoryRequest, _token);
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

                    _kernelManager.Add(quotation);
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
                            switch (response.MaintenanceResponse.ResultCode)
                            {
                                case ResultCode.Success:
                                    _nowDateTime = response.MaintenanceResponse.Datetime.ToDateTime().ToUniversalTime();
                                    _startDateTime = _nowDateTime - _distanceToThePast;
                                    break;
                                case ResultCode.Failure: Environment.Exit(0); break;
                                default: throw new ArgumentOutOfRangeException(nameof(response.MaintenanceResponse.ResultCode), @"Processor.ExecutiveProcessingTaskAsync: The provided response.MaintenanceResponse.ResultCode is not supported.");
                            }
                            break;
                        case MessageType.TradeCommand:
                            switch (response.TradeResponse.TradeCode)
                            {
                                case TradeCode.OpenPosition:
                                    _accountService.OpenPosition(response.TradeResponse.Ticket, response.TradeResponse.ResultCode, response.TradeResponse.Details);
                                    break;
                                case TradeCode.ClosePosition:
                                    _accountService.ClosePosition(response.TradeResponse.Ticket, response.TradeResponse.ResultCode, response.TradeResponse.Details);
                                    break;
                                case TradeCode.OpenTransaction:
                                    _accountService.OpenTransaction(response.TradeResponse.Ticket, response.TradeResponse.ResultCode, response.TradeResponse.Details);
                                    break;
                                case TradeCode.CloseTransaction:
                                    _accountService.CloseTransaction(response.TradeResponse.Ticket, response.TradeResponse.ResultCode, response.TradeResponse.Details);
                                    break;
                                case TradeCode.UpdatePosition:
                                    _accountService.UpdatePosition(response.TradeResponse.Ticket, response.TradeResponse.ResultCode, response.TradeResponse.Details);
                                    break;
                                default: throw new ArgumentOutOfRangeException(nameof(response.AccountInfoResponse.AccountInfoCode), @"Processor.ExecutiveProcessingTaskAsync: The provided response.TradeResponse.TradeCode is not supported.");
                            }
                            break;
                        case MessageType.AccountInfo:
                            switch (response.AccountInfoResponse.AccountInfoCode)
                            {
                                case AccountInfoCode.AccountProperties:
                                    _accountService.ProcessProperties(response.AccountInfoResponse.Details);
                                    _accountReady.SetResult(true);
                                    break;
                                case AccountInfoCode.TradingHistory:
                                    var positions = _accountService.ProcessPositionsHistory(response.AccountInfoResponse.Details);
                                    PositionsUpdated?.Invoke(this, new PositionsEventArgs(positions));
                                    break;
                                default: throw new ArgumentOutOfRangeException(nameof(response.AccountInfoResponse.AccountInfoCode), @"Processor.ExecutiveProcessingTaskAsync: The provided response.AccountInfoResponse.AccountInfoCode is not supported.");
                            }
                            break;
                        default: throw new ArgumentOutOfRangeException(nameof(response.Type), @"Processor.ExecutiveProcessingTaskAsync: The provided response.TradeType is not supported.");
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

    private Task RequestAccountPropertiesAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.AccountInfo,
            AccountInfoRequest = new AccountInfoRequest { AccountInfoCode = AccountInfoCode.AccountProperties, Ticket = 1 }
        };
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    public Task OpenPositionAsync(Symbol symbol, bool isReversed)
    {
        var request = _accountService.GetOpenPositionRequest(symbol, isReversed); 
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
    public Task ClosePositionAsync(Symbol symbol, bool isReversed)
    {
        var request = _accountService.GetClosePositionRequest(symbol, isReversed);
        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }

    public Task ExitAsync()
    {
        var request = new GeneralRequest
        {
            Type = MessageType.MaintenanceCommand,
            MaintenanceRequest = new MaintenanceRequest { MaintenanceCode = MaintenanceCode.CloseSession }
        };

        return _executiveCall.RequestStream.WriteAsync(request, _token);
    }
}