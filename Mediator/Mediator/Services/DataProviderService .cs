/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                      DataProviderService.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ticksdata;

namespace Mediator.Services;

internal sealed class DataProviderService : DataProvider.DataProviderBase, IDataProviderService
{
    private readonly CancellationTokenSource _cts;
    private readonly IDataService _dataService;
    private readonly ILogger<DataProviderService> _logger;

    private readonly string _dataProviderHost;
    private readonly int _dataProviderPort;
    private bool _isServiceActivated;
    private bool _isClientActivated;
    public event EventHandler<ActivationChangedEventArgs> IsServiceActivatedChanged = null!;
    public event EventHandler<ActivationChangedEventArgs> IsClientActivatedChanged = null!;

    public DataProviderService(IConfiguration configuration, CancellationTokenSource cts, IDataService dataService, ILogger<DataProviderService> logger)
    {
        _cts = cts;
        _dataService = dataService;
        _logger = logger;

        _dataProviderHost = configuration.GetValue<string>($"{nameof(_dataProviderHost)}")!;
        _dataProviderPort = configuration.GetValue<int>($"{nameof(_dataProviderPort)}");
    }   

    public bool IsServiceActivated
    {
        get => _isServiceActivated;
        set
        {
            if (value == _isServiceActivated)
            {
                return;
            }

            _isServiceActivated = value;
            IsServiceActivatedChanged(this, new ActivationChangedEventArgs(_isServiceActivated)); 
        }
    }

    public bool IsClientActivated
    {
        get => _isClientActivated;
        set
        {
            if (value == _isClientActivated)
            {
                return;
            }

            _isClientActivated = value;
            IsClientActivatedChanged(this, new ActivationChangedEventArgs(IsClientActivated));
        }
    }

    public async Task StartAsync()
    {
        try
        {
            var grpcServer = new Server
            {
                Services = { DataProvider.BindService(this) },
                Ports = { new ServerPort(_dataProviderHost, _dataProviderPort, ServerCredentials.Insecure) }
            };

            grpcServer.Start();

            _cts.Token.Register(() =>
            {
                var _ = Shutdown().ConfigureAwait(true);
            });

            try
            {
                IsServiceActivated = true;
                _logger.Log(LogLevel.Trace, "{TypeName} listening on {host}:{port}", GetType(), _dataProviderHost, _dataProviderPort);
                await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token triggers a cancellation
            }

            async Task Shutdown()
            {
                await grpcServer.ShutdownAsync().ConfigureAwait(true);
                IsServiceActivated = false;
                _logger.Log(LogLevel.Trace, "{TypeName} Shutted down.", GetType());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async override Task GetSinceDateTimeHourTillNowAsync(DataRequest request, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context)
    {
        IsClientActivated = true;

        var startDateTime = request.StartDateTime.ToDateTime();
        var quotations = await _dataService.GetSinceDateTimeHourTillNowAsync(startDateTime).ConfigureAwait(false);

        try
        {
            foreach (var quotation in quotations)
            {
                var protoQuotation = new Quotation
                {
                    Id = quotation.ID,
                    Symbol = ToProtoSymbol(quotation.Symbol),
                    Datetime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()),
                    Ask = quotation.Ask,
                    Bid = quotation.Bid,
                };

                var response = new DataResponse();
                response.Quotations.Add(protoQuotation);

                if (!context.CancellationToken.IsCancellationRequested)
                {
                    await responseStream.WriteAsync(response).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
        }
        catch (IOException ioEx)
        {
            IsClientActivated = false;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }

        IsClientActivated = false;
    }

    private static Symbol ToProtoSymbol(Common.Entities.Symbol symbol)
    {
        return symbol switch
        {
            Common.Entities.Symbol.EURGBP => Symbol.Eurgbp,
            Common.Entities.Symbol.EURJPY => Symbol.Eurjpy,
            Common.Entities.Symbol.EURUSD => Symbol.Eurusd,
            Common.Entities.Symbol.GBPJPY => Symbol.Gbpjpy,
            Common.Entities.Symbol.GBPUSD => Symbol.Gbpusd,
            Common.Entities.Symbol.USDJPY => Symbol.Usdjpy,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        };
    }
}