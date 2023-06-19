/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                      TicksDataProviderService.cs |
  +------------------------------------------------------------------+*/

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ticksdata;

namespace Mediator.Services;

internal class TicksDataProviderService : TicksDataProvider.TicksDataProviderBase, ITicksDataProviderService
{
    private readonly CancellationTokenSource _cts;
    private readonly IDataService _dataService;
    private readonly ILogger<TicksDataProviderService> _logger;

    private readonly string _host;
    private readonly int _port;

    public TicksDataProviderService(IConfiguration configuration, CancellationTokenSource cts, IDataService dataService, ILogger<TicksDataProviderService> logger)
    {
        _cts = cts;
        _dataService = dataService;
        _logger = logger;

        _host = configuration.GetValue<string>("TicksDataProviderHost")!;
        _port = configuration.GetValue<int>("TicksDataProviderPort");
    }

    public async Task StartAsync()
    {
        var grpcServer = new Server
        {
            Services = { TicksDataProvider.BindService(this) },
            Ports = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
        };

        grpcServer.Start();
        _logger.Log(LogLevel.Trace, "{TypeName} listening on {host}:{port}", GetType(), _host, _port);

        async Task Shutdown()
        {
            await grpcServer.ShutdownAsync().ConfigureAwait(false);
        }

        _cts.Token.Register(async () =>
        {
            await Shutdown().ConfigureAwait(false);
            _logger.Log(LogLevel.Trace, "{TypeName} stopped", GetType());
        });

        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Expected when the token triggers a cancellation
        }
        _logger.Log(LogLevel.Trace, "{TypeName} finished", GetType());
    }

    public async override Task<GetTicksResponse> GetTicksAsync(GetTicksRequest request, ServerCallContext context)
    {
        var startDateTime = request.StartDateTime.ToDateTime();
        var endDateTime = request.EndDateTime.ToDateTime();
        var quotations = await _dataService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
        var response = new GetTicksResponse();
        foreach (var quotation in quotations)
        {
            var protoQuotation = new Quotation
            {
                Id = quotation.ID,
                Symbol = ToProtoSymbol(quotation.Symbol),
                Datetime = Timestamp.FromDateTime(quotation.DateTime.ToUniversalTime()), // gRPC uses UTC time
                Ask = quotation.Ask,
                Bid = quotation.Bid,
            };
            response.Quotations.Add(protoQuotation);
        }
        return response;
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