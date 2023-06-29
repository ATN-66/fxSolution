/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                           DataProviderService.cs |
  +------------------------------------------------------------------+*/

using System.Reflection;
using Common.ExtensionsAndHelpers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ticksdata;

namespace Mediator.Services;

internal sealed class DataProviderService : DataProvider.DataProviderBase, IDataProviderService
{
    private const int RetryCountLimit = 5;
    private const int Backoff = 5;

    private readonly CancellationTokenSource _cts;
    private readonly IDataService _dataService;
    private readonly ILogger<DataProviderService> _logger;

    private readonly DataProviderSettings _dataProviderSettings;
    private bool? _isServiceActivated;
    private bool _isClientConnected;
    public event EventHandler<ThreeStateChangedEventArgs> IsServiceActivatedChanged = null!;
    public event EventHandler<TwoStateChangedEventArgs> IsClientActivatedChanged = null!;

    private static readonly Dictionary<Common.Entities.Symbol, Symbol> SymbolMapping = new()
    {
        { Common.Entities.Symbol.EURGBP, Symbol.Eurgbp },
        { Common.Entities.Symbol.EURJPY, Symbol.Eurjpy },
        { Common.Entities.Symbol.EURUSD, Symbol.Eurusd },
        { Common.Entities.Symbol.GBPJPY, Symbol.Gbpjpy },
        { Common.Entities.Symbol.GBPUSD, Symbol.Gbpusd },
        { Common.Entities.Symbol.USDJPY, Symbol.Usdjpy }
    };

    public DataProviderService(IOptions<DataProviderSettings> dataProviderSettings, CancellationTokenSource cts, IDataService dataService, ILogger<DataProviderService> logger)
    {
        _cts = cts;
        _dataService = dataService;
        _logger = logger;

        _isServiceActivated = false;
        _isClientConnected = false;

        _dataProviderSettings = dataProviderSettings.Value;
        _logger.LogTrace("{_dataProviderHost}:{_dataProviderPort}", _dataProviderSettings.Host, _dataProviderSettings.Port);
    }   

    public bool? IsServiceActivated
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

    public bool IsClientConnected
    {
        get => _isClientConnected;
        set
        {
            if (value == _isClientConnected)
            {
                return;
            }

            _isClientConnected = value;
            IsClientActivatedChanged(this, new TwoStateChangedEventArgs(IsClientConnected));
        }
    }

    public async Task StartAsync()
    {
        var retryCount = 0;

        while (retryCount < RetryCountLimit)
        {
            Server? grpcServer = null;
            try
            {
                grpcServer = new Server
                {
                    Services = { DataProvider.BindService(this) },
                    Ports = { new ServerPort(_dataProviderSettings.Host, _dataProviderSettings.Port, ServerCredentials.Insecure) }
                };

                grpcServer.Start();

                IsServiceActivated = true;
                _logger.LogTrace("Listening on {host}:{port}", _dataProviderSettings.Host, _dataProviderSettings.Port);
                await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);

                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled, shutting down gRPC server.");
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
    }

    public Task SaveQuotationsAsync(IEnumerable<Common.Entities.Quotation> quotations)
    {
        return _dataService.SaveQuotationsAsync(quotations);
    }

    public async override Task GetSinceDateTimeHourTillNowAsync(IAsyncStreamReader<DataRequest> requestStream, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context)
    {
        if (IsServiceActivated is null)
        {
            return;
        }

        var fault = false;
        IsClientConnected = true;

        if (!await requestStream.MoveNext().ConfigureAwait(false))
        {
            IsClientConnected = false;
            return;
        }

        var request = requestStream.Current;
        var startDateTime = request.StartDateTime.ToDateTime();
        try
        {
            var quotations = await _dataService.GetSinceDateTimeHourTillNowAsync(startDateTime).ConfigureAwait(false);
            var enumerable = quotations.ToList();
            if (!enumerable.Any())
            {
                await SendEndOfDataResponseAsync(responseStream).ConfigureAwait(false);
            }
            else
            {
                await SendQuotationsResponseAsync(responseStream, enumerable, context).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            await HandleDataServiceErrorAsync(responseStream, exception).ConfigureAwait(false);
            fault = true;
            throw;
        }
        finally
        {
            CleanupAfterStreaming(fault);
        }
    }

    private async Task HandleDataServiceErrorAsync(IAsyncStreamWriter<DataResponse> responseStream, Exception exception)
    {
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

    private static Task SendEndOfDataResponseAsync(IAsyncStreamWriter<DataResponse> responseStream)
    {
        var endOfDataResponse = new DataResponse
        {
            Status = new DataResponseStatus
            {
                Code = DataResponseStatus.Types.StatusCode.EndOfData,
                Details = "No more data available."
            }
        };

        return responseStream.WriteAsync(endOfDataResponse);
    }

    private static async Task SendQuotationsResponseAsync(IAsyncStreamWriter<DataResponse> responseStream, IEnumerable<Common.Entities.Quotation> quotations, ServerCallContext context)
    {
        foreach (var quotation in quotations)
        {
            var response = new DataResponse
            {
                Status = new DataResponseStatus
                {
                    Code = DataResponseStatus.Types.StatusCode.Ok
                },

                Quotations =
                {
                    new Quotation
                    {
                        Id = quotation.ID,
                        Symbol = ToProtoSymbol(quotation.Symbol),
                        Datetime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()),
                        Ask = quotation.Ask,
                        Bid = quotation.Bid,
                    }
                }
            };

            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            await responseStream.WriteAsync(response).ConfigureAwait(false);
        }
    }

    private void CleanupAfterStreaming(bool fault)
    {
        IsClientConnected = false;
        if (fault)
        {
            IsServiceActivated = null;
        }
    }

    private static Symbol ToProtoSymbol(Common.Entities.Symbol symbol)
    {
        if (!SymbolMapping.TryGetValue(symbol, out var protoSymbol))
        {
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
        }

        return protoSymbol;
    }
}