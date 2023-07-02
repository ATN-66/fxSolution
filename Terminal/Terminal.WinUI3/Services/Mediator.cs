/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;
using Ticksdata;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Services;

public class Mediator : IMediator
{
    private readonly ILogger<Mediator> _logger;
    
    private readonly ConcurrentDictionary<DateTime, List<Quotation>> _hoursCache = new();
    private readonly ConcurrentQueue<DateTime> _hoursKeys = new();
    private DateTime _currentHoursKey;

    private readonly int _maxHoursInCache;
    private readonly int _deadline;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;

    private readonly string _grpcChannelAddress;

    private static readonly Dictionary<Ticksdata.Symbol, Symbol> SymbolMapping = new()
    {
        { Ticksdata.Symbol.EurGbp, Symbol.EURGBP },
        { Ticksdata.Symbol.EurJpy , Symbol.EURJPY },
        { Ticksdata.Symbol.EurUsd , Symbol.EURUSD },
        { Ticksdata.Symbol.GbpJpy , Symbol.GBPJPY },
        { Ticksdata.Symbol.GbpUsd , Symbol.GBPUSD },
        { Ticksdata.Symbol.UsdJpy , Symbol.USDJPY }
    };

    public Mediator(IConfiguration configuration, ILogger<Mediator> logger)
    {
        _logger = logger;

        _grpcChannelAddress = configuration.GetValue<string>($"{nameof(_grpcChannelAddress)}")!;
        _maxHoursInCache = configuration.GetValue<int>($"{nameof(_maxHoursInCache)}");
        _deadline = configuration.GetValue<int>($"{nameof(_deadline)}");

        _maxSendMessageSize = 50 * 1024 * 1024; //e.g. 50 MB //todo:
        _maxReceiveMessageSize = 50 * 1024 * 1024; //e.g. 50 MB //todo:

        //todo:log
}

    public async Task<IEnumerable<Quotation>> GetHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        var difference = Math.Ceiling((endDateTimeInclusive - startDateTimeInclusive).TotalHours);
        if (difference > _maxHoursInCache)
        {
            throw new InvalidOperationException($"Requested hours exceed maximum cache size of {_maxHoursInCache} hours.");
        }
        if (difference < 0)
        {
            throw new InvalidOperationException("Start date cannot be later than end date.");
        }

        var tasks = new List<Task>();
        var quotations = new List<Quotation>();
        var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
        var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour).AddHours(1);
        _currentHoursKey = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
        var key = start;
        do
        {
            if (!_hoursCache.ContainsKey(key) || _currentHoursKey.Equals(key))
            {
                tasks.Add(LoadHistoricalDataAsync(key, key));
            }

            key = key.Add(new TimeSpan(1, 0, 0));
        }
        while (key < end);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        key = start;
        do
        {
            if (_hoursCache.ContainsKey(key))
            {
                quotations.AddRange(GetData(key));
            }
            else
            {
                throw new NotImplementedException("Key is absent.");
            }

            key = key.Add(new TimeSpan(1, 0, 0));
        }
        while (key < end);
        return quotations;
    }
    private async Task LoadHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        if (!startDateTimeInclusive.Equals(endDateTimeInclusive))
        {
            throw new InvalidOperationException("Start and end times must be the same. This function only supports processing of a single hour at a time.");
        }

        try
        {
            var quotations = await GetDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            ProcessData(startDateTimeInclusive, quotations);
        }
        catch (RpcException rpcException)
        {
            // "Unavailable", "Error connecting to subchannel."
            LogExceptionHelper.LogException(_logger, rpcException, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
        catch (SocketException socketException)
        {
            // "System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it."
            LogExceptionHelper.LogException(_logger, socketException, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }

        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    private async Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        var channelOptions = new GrpcChannelOptions
        {
            MaxSendMessageSize = _maxSendMessageSize,
            MaxReceiveMessageSize = _maxReceiveMessageSize
        };

        using var channel = GrpcChannel.ForAddress(_grpcChannelAddress, channelOptions);
        var client = new DataProvider.DataProviderClient(channel);
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(_deadline)));
        var request = new DataRequest 
        { 
           StartDateTime = Timestamp.FromDateTime(startDateTimeInclusive.ToUniversalTime()),
           EndDateTime = Timestamp.FromDateTime(endDateTimeInclusive.ToUniversalTime()),
           Code = DataRequest.Types.StatusCode.HistoricalData 
        };

        try
        {
            var call = client.GetDataAsync(callOptions);
            await call.RequestStream.WriteAsync(request).ConfigureAwait(false);
            await call.RequestStream.CompleteAsync().ConfigureAwait(false);

            int counter = default;
            IList<Quotation> quotations = new List<Quotation>();

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                switch (response.Status.Code)
                {
                    case DataResponseStatus.Types.StatusCode.Ok:
                        foreach (var item in response.Quotations)
                        {
                            var quotation = new Quotation(counter++, ToEntitiesSymbol(item.Symbol), item.Datetime.ToDateTime().ToUniversalTime(), item.Ask, item.Bid);
                            quotations.Add(quotation);
                        }
                        break;
                    case DataResponseStatus.Types.StatusCode.NoData:
                        break;
                    case DataResponseStatus.Types.StatusCode.ServerError:
                        throw new Exception($"Server error: {response.Status.Details}");
                    default:
                        throw new Exception($"Unknown status code: {response.Status.Code}");
                }
            }

            return quotations;
        }
        catch (RpcException rpcException)
        {
            LogExceptionHelper.LogException(_logger, rpcException, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    private void ProcessData(DateTime dateTime, IEnumerable<Quotation> quotations)
    {
        var done = false;
        var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.DateTime.Year });
        foreach (var yearGroup in groupedByYear)
        {
            var year = yearGroup.Key.Year;
            var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.DateTime.Month });
            foreach (var monthGroup in groupedByMonth)
            {
                var month = monthGroup.Key.Month;
                var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.DateTime.Day });
                foreach (var dayGroup in groupedByDay)
                {
                    var day = dayGroup.Key.Day;
                    var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.DateTime.Hour });
                    foreach (var hourGroup in groupedByHour)
                    {
                        var hour = hourGroup.Key.Hour;
                        var key = new DateTime(year, month, day, hour, 0, 0);
                        var quotationsToSave = hourGroup.ToList();
                        SetData(key, quotationsToSave);
                        done = true;
                        _logger.LogTrace("year:{year}, month:{month:D2}, day:{day:D2}, hour:{hour:D2}. Count:{quotationsToSaveCount}", year.ToString(), month.ToString(), day.ToString(), hour.ToString(), quotationsToSave.Count.ToString());
                    }
                }
            }
        }

        if (!done)
        {
            SetData(dateTime, quotations: new List<Quotation>());
        }
    }
    private void AddData(DateTime key, List<Quotation> quotations)
    {
        if (_hoursCache.Count >= _maxHoursInCache)
        {
            if (_hoursKeys.TryDequeue(out var oldestKey))
            {
                _hoursCache.TryRemove(oldestKey, out _);
            }
        }

        if (!_hoursCache.ContainsKey(key))
        {
            _hoursCache[key] = quotations;
            _hoursKeys.Enqueue(key);
        }
        else
        {
            if (!_currentHoursKey.Equals(key))
            {
                throw new InvalidOperationException("the key already exists.");
            }

            _hoursCache[key] = quotations;
        }
    }
    private void SetData(DateTime key, List<Quotation> quotations)
    {
        AddData(key, quotations);
    }
    private IEnumerable<Quotation> GetData(DateTime key)
    {
        _hoursCache.TryGetValue(key, out var quotations);
        return quotations!;
    }
    private static Symbol ToEntitiesSymbol(Ticksdata.Symbol protoSymbol)
    {
        if (!SymbolMapping.TryGetValue(protoSymbol, out var symbol))
        {
            throw new ArgumentOutOfRangeException(nameof(protoSymbol), protoSymbol, null);
        }

        return symbol;
    }
}