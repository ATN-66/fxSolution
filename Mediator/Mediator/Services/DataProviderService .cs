﻿/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                           DataProviderService.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;
using System.Reflection;
using Common.Entities;
using System.Timers;
using Common.ExtensionsAndHelpers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ticksdata;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Timer = System.Timers.Timer;
using Enum = System.Enum;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Mediator.Services;

internal sealed class DataProviderService : DataProvider.DataProviderBase, IDataProviderService
{
    private delegate void TickDelegate(Quotation quotation, int counter); 
    private TickDelegate _tick = (_, _) => { };

    private readonly Guid _guid = Guid.NewGuid();
    private readonly MainViewModel _mainViewModel;
    private readonly CancellationTokenSource _cts;
    private readonly IDataBaseService _dataBaseService;
    private readonly ILogger<IDataProviderService> _logger;
    private readonly DataProviderSettings _dataProviderSettings;

    private readonly SemaphoreSlim _mutex = new(1);
    private readonly ReaderWriterLockSlim _queueLock = new();

    private readonly Quotation[] _lastKnownQuotations = new Quotation[TotalIndicators];
    private BlockingCollection<(int id, int symbol, string datetime, double ask, double bid)> _quotations = new();
    private BlockingCollection<Quotation>? _quotationsToSend;
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();
    private int[] _counter = new int[TotalIndicators];

    private const string Ok = "ok";
    private readonly string _mT5DateTimeFormat;

    private const int BatchSize = 1000;
    private const int Minutes = 10;
    private readonly Timer _saveTimer;

    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;

    private const int RetryCountLimit = 5;
    private const int Backoff = 5;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private static volatile bool _isFaulted;
   
    public event EventHandler<ThreeStateChangedEventArgs> IsServiceActivatedChanged = null!;
    public event EventHandler<TwoStateChangedEventArgs> IsClientActivatedChanged = null!;

    private static readonly Dictionary<Symbol, Ticksdata.Symbol> SymbolMapping = new()
    {
        { Symbol.EURGBP, Ticksdata.Symbol.EurGbp },
        { Symbol.EURJPY, Ticksdata.Symbol.EurJpy },
        { Symbol.EURUSD, Ticksdata.Symbol.EurUsd },
        { Symbol.GBPJPY, Ticksdata.Symbol.GbpJpy },
        { Symbol.GBPUSD, Ticksdata.Symbol.GbpUsd },
        { Symbol.USDJPY, Ticksdata.Symbol.UsdJpy }
    };

    public DataProviderService(IConfiguration configuration, IOptions<DataProviderSettings> dataProviderSettings, MainViewModel mainViewModel, CancellationTokenSource cts, IDataBaseService dataBaseService, ILogger<IDataProviderService> logger)
    {
        _mainViewModel = mainViewModel;
        IsServiceActivatedChanged += (_, e) => { _mainViewModel.IsDataProviderActivated = e.IsActivated; };
        IsClientActivatedChanged += (_, e) => { _mainViewModel.IsDataClientActivated = e.IsActivated; };
        _logger = logger;

        _mT5DateTimeFormat = configuration.GetValue<string>($"{nameof(_mT5DateTimeFormat)}")!;
        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");

        _saveTimer = new Timer(Minutes * 15 * 1000);
        _saveTimer.Elapsed += OnSaveTimerElapsedAsync;
        _saveTimer.AutoReset = true;
        _saveTimer.Enabled = true;

        void ProcessQuotationsAction() => Task.Run(() => ProcessAsync(cts.Token), cts.Token).ConfigureAwait(false);
        _mainViewModel.InitializationComplete += ProcessQuotationsAction;
        _tick += _mainViewModel.SetIndicator;

        _cts = cts;
        _dataBaseService = dataBaseService;
        _logger = logger;

        _isServiceActivated = false;
        _isClientConnected = false;

        _dataProviderSettings = dataProviderSettings.Value;
        _logger.LogTrace("{_guid} is ON. {_dataProviderHost}:{_dataProviderPort}", _guid, _dataProviderSettings.Host, _dataProviderSettings.Port);
    }

    private bool? _isServiceActivated;
    private bool? IsServiceActivated
    {
        get => _isServiceActivated;
        set
        {
            if (value == _isServiceActivated)
            {
                return;
            }

            _isServiceActivated = value;
            IsServiceActivatedChanged(this, new ThreeStateChangedEventArgs(_isServiceActivated));
        }
    }

    #region ClientConnection
    private int _clientConnectionCounter;
    private bool _isClientConnected;
    private bool IsClientConnected
    {
        set
        {
            _isClientConnected = value;
            var newValue = _clientConnectionCounter > 0;
            if (newValue == _isClientConnected)
            {
                return;
            }

            _isClientConnected = newValue;
            IsClientActivatedChanged(this, new TwoStateChangedEventArgs(_isClientConnected));
        }
    }
    private void ConnectClient()
    {
        Interlocked.Increment(ref _clientConnectionCounter);
        IsClientConnected = _isClientConnected;
    }
    private void DisconnectClient()
    {
        if (_clientConnectionCounter > 0)
        {
            Interlocked.Decrement(ref _clientConnectionCounter);
        }
        IsClientConnected = _isClientConnected;
    }
    #endregion ClientConnection

    public async Task StartAsync()
    {
        var retryCount = 0;

        while (retryCount < RetryCountLimit)
        {
            Server? grpcServer = null;
            _isFaulted = false;

            try
            {
                grpcServer = new Server(new List<ChannelOption>
                {
                    new("grpc.max_send_message_length", _maxSendMessageSize), 
                    new("grpc.max_receive_message_length", _maxReceiveMessageSize)

                })
                {
                    Services = { DataProvider.BindService(this) },
                    Ports = { new ServerPort(_dataProviderSettings.Host, _dataProviderSettings.Port, ServerCredentials.Insecure) },
                };

                grpcServer.Start();

                IsServiceActivated = true;
                _logger.LogTrace("Listening on {host}:{port}", _dataProviderSettings.Host, _dataProviderSettings.Port.ToString());
                await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);

                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled, shutting down and restarting gRPC server...");
                break;  // Break out of the loop on cancellation, assuming we don't want to retry in this case
            }
            catch (IOException ioException)
            {
                LogExceptionHelper.LogException(_logger, ioException, MethodBase.GetCurrentMethod()!.Name, "");
                retryCount++;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
                retryCount++;
            }
            finally
            {
                if (grpcServer != null)
                {
                    await grpcServer.ShutdownAsync().ConfigureAwait(true);
                    IsServiceActivated = false;
                    _logger.LogTrace("Shutted down.");
                }
            }

            if (retryCount > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Backoff * retryCount)).ConfigureAwait(false);
            }
        }

        if (retryCount >= RetryCountLimit)
        {
            _mainViewModel.AtFault = true;
            _logger.LogCritical("CRITICAL ERROR: The gRPC server has repeatedly failed to start after {retryCount} attempts. This indicates a severe underlying issue that needs immediate attention. The server will not try to restart again. Please check the error logs for more information and take necessary action immediately.", retryCount);
        }
    }

    #region Indicators
    public async Task DeInitAsync(int reason)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_mainViewModel.IndicatorsConnected)
            {
                return;
            }

            if (!_quotations.IsAddingCompleted)
            {
                _quotations.CompleteAdding();
            }

            while (!_quotations.IsCompleted)
            {
                Task.Delay(1000).ConfigureAwait(false).GetAwaiter();
            }

            await SaveQuotationsAsync().ConfigureAwait(false);
            for (var i = 0; i < TotalIndicators; i++)
            {
                _lastKnownQuotations[i] = Quotation.Empty;
            }

            await _mainViewModel.IndicatorDisconnectedAsync((DeInitReason)reason, _counter.Sum()).ConfigureAwait(false);
            _quotations = new();
            _counter = new int[TotalIndicators];
        }
        catch (Exception exception)
        {
            _mainViewModel.AtFault = true;
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
        finally
        {
            _mutex.Release();
        }
    }
    public async Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace)
    {
        const int maxRetries = 5;
        var retryCount = 0;

        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            while (_mainViewModel.IndicatorsConnected && retryCount < maxRetries)
            {
                await Task.Delay(2000).ConfigureAwait(false);
                retryCount++;
            }

            if (retryCount == maxRetries)
            {
                throw new TimeoutException("The operation timed out.");
            }

            if (_lastKnownQuotations[symbol - 1] == Quotation.Empty)
            {
                await _mainViewModel.IndicatorConnectedAsync((Symbol)symbol, (Workplace)workplace).ConfigureAwait(false);
                var resultSymbol = (Symbol)symbol;
                var resultDateTime = DateTime.ParseExact(datetime, _mT5DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
                var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid);
                _lastKnownQuotations[symbol - 1] = quotation;
                _quotations.Add((id, symbol, datetime, ask, bid));
                _mainViewModel.SetIndicator(quotation, 0);
            }
            else
            {
                throw new InvalidOperationException("An indicator is eligible for a single connection only.");
            }
        }
        catch (Exception exception)
        {
            _mainViewModel.AtFault = true;
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
        finally
        {
            _mutex.Release();
        }

        return Ok;
    }
    public string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        _quotations.Add((id, symbol, datetime, ask, bid));
        return Ok;
    }
    private async Task ProcessAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var (id, symbol, datetime, ask, bid) in _quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
                Process(id, symbol, datetime, ask, bid);
            }

            // If cancellation has been requested, save remaining data
            if (ct.IsCancellationRequested)
            {
                await SaveQuotationsAsync().ConfigureAwait(false);  // You might want to await this without .ConfigureAwait(false) if the continuation depends on the context
            }
        }
        catch (Exception exception)
        {
            _mainViewModel.AtFault = true;
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    private void Process(int id, int symbol, string datetime, double ask, double bid)
    {
        try
        {
            var resultSymbol = (Symbol)symbol;
            var resultDateTime = DateTime.ParseExact(datetime, _mT5DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            while (_lastKnownQuotations[symbol - 1].DateTime >= resultDateTime)
            {
                resultDateTime = resultDateTime.AddMilliseconds(1);
            }
            _counter[symbol - 1] += 1;

            var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid);
            _tick(quotation, _counter[symbol - 1]);

            bool shouldSave;
            _queueLock.EnterWriteLock();
            try
            {
                _lastKnownQuotations[symbol - 1] = quotation;
                _quotationsToSave.Enqueue(quotation);
                shouldSave = _quotationsToSave.Count >= BatchSize;
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }

            if (shouldSave)
            {
                SaveQuotationsAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            _mainViewModel.AtFault = true;
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    private void OnSaveTimerElapsedAsync(object? sender, ElapsedEventArgs e)
    {
        SaveQuotationsAsync().ConfigureAwait(false);
    }
    private async Task SaveQuotationsAsync()
    {
        int count;
        var quotationsToSave = new List<Quotation>();

        _queueLock.EnterReadLock();
        try
        {
            while (_quotationsToSave.TryDequeue(out var quotation))
            {
                quotationsToSave.Add(quotation);
            }

            count = quotationsToSave.Count;
        }
        finally
        {
            _queueLock.ExitReadLock();
        }

        if (quotationsToSave.Count == 0)
        {
            return;
        }

        try
        {
            var result = await _dataBaseService.SaveDataAsync(quotationsToSave).ConfigureAwait(false);
            if (result != count)
            {
                throw new Exception("Not all data saved.");
            }
        }
        catch (Exception exception)
        {
            _mainViewModel.AtFault = true;
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "error with _dataBaseService.SaveDataAsync");
            throw;
        }
    }
    #endregion Indicators

    private void AddTicks(Quotation quotation, int count)
    {
        _quotationsToSend!.Add(quotation);
    }

    #region Terminal
    public async override Task GetDataAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context)
    {
        if (IsServiceActivated is null || _isFaulted)
        {
            return;
        }

        ConnectClient();

        if (!await requestStream.MoveNext().ConfigureAwait(false))
        {
            DisconnectClient();
            return;
        }

        var request = requestStream.Current;
        var status = request.Code;

        switch (status)
        {
            case DataRequest.Types.StatusCode.HistoricalData:
            {
                var startDateTime = request.StartDateTime.ToDateTime();

                try
                {
                    await SaveQuotationsAsync().ConfigureAwait(false);
                    var quotations = await _dataBaseService.GetHistoricalDataAsync(startDateTime, startDateTime).ConfigureAwait(false);
                    var quotationsToSend = quotations.ToList();
                    if (!quotationsToSend.Any())
                    {
                        await SendNoDataResponseAsync(responseStream).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendDataResponseAsync(responseStream, quotationsToSend, context).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Operation cancelled...");
                }
                catch (RpcException)
                {
                    _logger.LogInformation("RpcException...please investigate.");
                    throw;
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    _isFaulted = true;
                    await HandleDataServiceErrorAsync(responseStream, invalidOperationException).ConfigureAwait(false);
                    throw;
                }
                catch (Exception exception)
                {
                    _isFaulted = true;
                    await HandleDataServiceErrorAsync(responseStream, exception).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    CleanupAfterStreaming();
                }

                break;
            }
            case DataRequest.Types.StatusCode.BufferedData:
                try
                {
                    var quotations = new List<Quotation>();
                    _queueLock.EnterReadLock();
                    while (_quotationsToSave.TryDequeue(out var quotation))
                    {
                        quotations.Add(quotation);
                    }
                    _queueLock.ExitReadLock();

                    if (!quotations.Any())
                    {
                        await SendNoDataResponseAsync(responseStream).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendDataResponseAsync(responseStream, quotations, context).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Operation cancelled...");
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    _isFaulted = true;
                    await HandleDataServiceErrorAsync(responseStream, invalidOperationException).ConfigureAwait(false);
                    throw;
                }
                catch (Exception exception)
                {
                    _isFaulted = true;
                    await HandleDataServiceErrorAsync(responseStream, exception).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    CleanupAfterStreaming();
                }

                break;
            case DataRequest.Types.StatusCode.LiveData:
                _quotationsToSend = new BlockingCollection<Quotation>();
                _tick += AddTicks;

                try
                {
                    await foreach (var quotation in _quotationsToSend.GetConsumingAsyncEnumerable(context.CancellationToken).WithCancellation(context.CancellationToken))
                    {
                        var response = new DataResponse
                        {
                            Status = new DataResponseStatus
                            {
                                Code = DataResponseStatus.Types.StatusCode.Ok
                            }
                        };

                        var q = new Ticksdata.Quotation { Id = quotation.ID, Symbol = ToProtoSymbol(quotation.Symbol), Datetime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()), Ask = quotation.Ask, Bid = quotation.Bid };

                        response.Quotations.Add(q);

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            //from client?
                            break;
                        }

                        await responseStream.WriteAsync(response).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Operation cancelled...");
                }
                catch (RpcException)
                {
                    _logger.LogInformation("RpcException...please investigate.");
                    throw;
                }
                catch (Exception exception)
                {
                    _isFaulted = true;
                    await HandleDataServiceErrorAsync(responseStream, exception).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    _tick -= AddTicks;
                    CleanupAfterStreaming();
                }
                break;
            case DataRequest.Types.StatusCode.StopData:
            default: throw new InvalidOperationException();
        }
    }
    private Task SendNoDataResponseAsync(IAsyncStreamWriter<DataResponse> responseStream)
    {
        var endOfDataResponse = new DataResponse
        {
            Status = new DataResponseStatus
            {
                Code = DataResponseStatus.Types.StatusCode.NoData,
                Details = "No more / No data available."
            }
        };

        _logger.LogTrace("{info}", "No more / No data available.");
        return responseStream.WriteAsync(endOfDataResponse);
    }
    private static Task SendDataResponseAsync(IAsyncStreamWriter<DataResponse> responseStream, IEnumerable<Quotation> quotations, ServerCallContext context)
    {
        var response = new DataResponse
        {
            Status = new DataResponseStatus
            {
                Code = DataResponseStatus.Types.StatusCode.Ok
            },
        };

        foreach (var quotation in quotations)
        {
            response.Quotations.Add(new Ticksdata.Quotation
            {
                Id = quotation.ID,
                Symbol = ToProtoSymbol(quotation.Symbol),
                Datetime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()),
                Ask = quotation.Ask,
                Bid = quotation.Bid
            });

            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return responseStream.WriteAsync(response);
    }
    private async Task HandleDataServiceErrorAsync(IAsyncStreamWriter<DataResponse> responseStream, Exception exception)
    {
        _mainViewModel.AtFault = true;
        var exceptionResponse = new DataResponse
        {
            Status = new DataResponseStatus
            {
                Code = DataResponseStatus.Types.StatusCode.ServerError,
                Details = exception.Message
            }
        };
        await responseStream.WriteAsync(exceptionResponse).ConfigureAwait(false);
        LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "Error during getting data from database or communication error.");
    }
    private void CleanupAfterStreaming()
    {
        DisconnectClient();
        if (_isFaulted)
        {
            IsServiceActivated = null;
        }
    }
    private static Ticksdata.Symbol ToProtoSymbol(Symbol symbol)
    {
        if (!SymbolMapping.TryGetValue(symbol, out var protoSymbol))
        {
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
        }

        return protoSymbol;
    }
    #endregion Terminal
}