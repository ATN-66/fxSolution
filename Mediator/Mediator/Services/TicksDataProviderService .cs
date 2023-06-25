/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                      TicksDataProviderService.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABI.Windows.UI.WebUI;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ticksdata;

namespace Mediator.Services;

internal sealed class TicksDataProviderService : TicksDataProvider.TicksDataProviderBase, ITicksDataProviderService
{
    private readonly CancellationTokenSource _cts;
    private readonly IDataService _dataService;
    private readonly ILogger<TicksDataProviderService> _logger;

    private readonly string _host;
    private readonly int _port;
    private bool _isActivated;
    public event EventHandler<ActivationChangedEventArgs> IsActivatedChanged = null!;

    public TicksDataProviderService(IConfiguration configuration, CancellationTokenSource cts, IDataService dataService, ILogger<TicksDataProviderService> logger)
    {
        _cts = cts;
        _dataService = dataService;
        _logger = logger;

        _host = configuration.GetValue<string>("TicksDataProviderHost")!;
        _port = configuration.GetValue<int>("TicksDataProviderPort");
    }

    public bool IsActivated
    {
        get => _isActivated;
        set
        {
            if (value == _isActivated)
            {
                return;
            }

            _isActivated = value;
            IsActivatedChanged?.Invoke(this, new ActivationChangedEventArgs(_isActivated)); IsActivatedChanged?.Invoke(this, new ActivationChangedEventArgs(_isActivated));
        }
    }

    public async Task StartAsync()
    {
        var grpcServer = new Server
        {
            Services = { TicksDataProvider.BindService(this) },
            Ports = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        IsActivated = true;
        _logger.Log(LogLevel.Trace, "{TypeName} listening on {host}:{port}", GetType(), _host, _port);

        async Task Shutdown()
        {
            await grpcServer.ShutdownAsync().ConfigureAwait(false);
            IsActivated = false;
        }

        _cts.Token.Register(() =>
        {
            var _ = Shutdown().ConfigureAwait(false);
            _logger.Log(LogLevel.Trace, "{TypeName} stopped", GetType());
        });

        try
        {
            IsActivated = true;
            await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Expected when the token triggers a cancellation
        }
        _logger.Log(LogLevel.Trace, "{TypeName} finished", GetType());
    }

    public async override Task GetTicksAsync(GetTicksRequest request, IServerStreamWriter<GetTicksResponse> responseStream, ServerCallContext context)
    {
        var startDateTime = request.StartDateTime.ToDateTime();
        var quotations = await _dataService.GetTicksAsync(startDateTime).ConfigureAwait(false);

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

            var response = new GetTicksResponse();
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