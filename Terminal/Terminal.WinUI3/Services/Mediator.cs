/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;
using Common.Entities;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;
using Ticksdata;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.Services;

public class Mediator : IMediator
{
    private readonly ILogger<DataService> _logger;
    private readonly Dictionary<string, List<Quotation>> _ticksCache = new();
    private readonly Queue<string> _keys = new();
    private const int MaxItems = 744; //(24*31)
    private string _currentHoursKey = null!;
    private readonly string _grpcChannelAddress;
    private readonly int _deadline;

    public Mediator(IConfiguration configuration, ILogger<DataService> logger)
    {
        _logger = logger;

        _grpcChannelAddress = configuration.GetValue<string>($"{nameof(_grpcChannelAddress)}")!;
        _deadline = configuration.GetValue<int>($"{nameof(_deadline)}")!;
    }

    public async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        try
        {
            var quotations = new List<Quotation>();
            var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
            var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour);
            _currentHoursKey = $"{end.Year}.{end.Month:D2}.{end.Day:D2}.{end.Hour:D2}";
            end = end.AddHours(1);

            var index = start;
            do
            {
                var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";
                if (!_ticksCache.ContainsKey(key))
                {
                    await LoadSinceDateTimeHourTillNowAsync(index).ConfigureAwait(false);
                    if (!_ticksCache.ContainsKey(key))
                    {
                        SetQuotations(key, new List<Quotation>());
                    }
                }

                if (_ticksCache.ContainsKey(key))
                {
                    quotations.AddRange(GetQuotations(key));
                }

                index = index.Add(new TimeSpan(1, 0, 0));
            }
            while (index < end);

            return quotations;
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }
    }
    private async Task LoadSinceDateTimeHourTillNowAsync(DateTime startDateTimeInclusive)
    {
        try
        {
            _logger.LogError("{Type}: trying to connect to: {address}", GetType(), _grpcChannelAddress);
            using var channel = GrpcChannel.ForAddress(_grpcChannelAddress);
            var client = new DataProvider.DataProviderClient(channel);
            var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(60 * 5))); //todo

            var request = new DataRequest { StartDateTime = Timestamp.FromDateTime(startDateTimeInclusive.ToUniversalTime()) };
            var call = client.GetSinceDateTimeHourTillNowAsync(request, callOptions);

            int counter = default;
            IList<Quotation> quotations = new List<Quotation>();
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                foreach (var item in response.Quotations)
                {
                    var quotation = new Quotation(counter++, ToEntitiesSymbol(item.Symbol), item.Datetime.ToDateTime().ToUniversalTime(), item.Ask, item.Bid);
                    quotations.Add(quotation);
                }
            }

            var end = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
            _currentHoursKey = $"{end.Year}.{end.Month:D2}.{end.Day:D2}.{end.Hour:D2}";

            //var yearsCounter = 0;
            //var monthsCounter = 0;
            //var daysCounter = 0;
            //var hoursCounter = 0;

            var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.DateTime.Year });
            foreach (var yearGroup in groupedByYear)
            {
                var year = yearGroup.Key.Year;
                //yearsCounter++;
                var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.DateTime.Month });
                foreach (var monthGroup in groupedByMonth)
                {
                    var month = monthGroup.Key.Month;
                    //monthsCounter++;
                    var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.DateTime.Day });
                    foreach (var dayGroup in groupedByDay)
                    {
                        var day = dayGroup.Key.Day;
                        //daysCounter++;
                        var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.DateTime.Hour });
                        foreach (var hourGroup in groupedByHour)
                        {
                            var hour = hourGroup.Key.Hour;
                            //hoursCounter++;
                            var key = $"{year}.{month:D2}.{day:D2}.{hour:D2}";
                            SetQuotations(key, hourGroup.ToList());
                            //Debug.WriteLine(key);
                        }
                    }
                }
            }

            //Debug.WriteLine($"year counter:{yearsCounter}, month counter:{monthsCounter:D2}, day counter:{daysCounter:D2}, hour counter:{hoursCounter:D2}");
        }
        catch (RpcException rpcException)
        {
            _logger.LogError("{Message}", rpcException.Message);
            _logger.LogError("{Message}", rpcException.InnerException?.Message);

            // "Unavailable", "Error connecting to subchannel."
            throw;
        }
        catch (System.Net.Sockets.SocketException socketException)
        {
            _logger.LogError("{Message}", socketException.Message);
            _logger.LogError("{Message}", socketException.InnerException?.Message);

            // "System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it."
            throw;
        }

        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }
    }
    private void AddQuotation(string key, List<Quotation> quotationList)
    {
        if (_ticksCache.Count >= MaxItems)
        {
            var oldestKey = _keys.Dequeue();
            _ticksCache.Remove(oldestKey);
        }

        if (string.Equals(_currentHoursKey, key))
        {
            return;
        }

        _ticksCache[key] = quotationList;
        _keys.Enqueue(key);
    }
    private void SetQuotations(string key, List<Quotation> quotations)
    {
        AddQuotation(key, quotations);
    }
    private IEnumerable<Quotation> GetQuotations(string key)
    {
        _ticksCache.TryGetValue(key, out var quotations);
        return quotations!;
    }
    private static Common.Entities.Symbol ToEntitiesSymbol(Ticksdata.Symbol symbol)
    {
        return symbol switch
        {
            Ticksdata.Symbol.Eurgbp => Common.Entities.Symbol.EURGBP,
            Ticksdata.Symbol.Eurusd => Common.Entities.Symbol.EURUSD,
            Ticksdata.Symbol.Gbpusd => Common.Entities.Symbol.GBPUSD,
            Ticksdata.Symbol.Usdjpy => Common.Entities.Symbol.USDJPY,
            Ticksdata.Symbol.Eurjpy => Common.Entities.Symbol.EURJPY,
            Ticksdata.Symbol.Gbpjpy => Common.Entities.Symbol.GBPJPY,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        };
    }
}