/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.Entities;
using Grpc.Core;
using Grpc.Net.Client;
using Fx.Grpc;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

var cancellationTokenSource = new CancellationTokenSource();
var liveDataQueue = new BlockingCollection<Quotation>();

ConcurrentDictionary<DateTime, List<Quotation>> hoursCache = new();
ConcurrentQueue<DateTime> hoursKeys = new();
//DateTime currentHoursKey;

const int deadline = int.MaxValue;
//const int maxHoursInCache = 168;
//const string grpcChannelAddress = "http://192.168.50.78:49051";
const string grpcChannelAddress = "http://192.168.50.111:49051";
const int maxSendMessageSize = 50 * 1024 * 1024; //e.g. 50 MB wo 4
const int maxReceiveMessageSize = 50 * 1024 * 1024; //e.g. 50 MB wo 4

// ReSharper disable once JoinDeclarationAndInitializer
//DateTime endTime;
// ReSharper disable once JoinDeclarationAndInitializer
//DateTime startTime;
// ReSharper disable once JoinDeclarationAndInitializer
IEnumerable<Quotation> result;

var symbolMapping = new Dictionary<Fx.Grpc.Symbol, Symbol>
{
    { Fx.Grpc.Symbol.EurGbp, Symbol.EURGBP },
    { Fx.Grpc.Symbol.EurJpy, Symbol.EURJPY },
    { Fx.Grpc.Symbol.EurUsd, Symbol.EURUSD },
    { Fx.Grpc.Symbol.GbpJpy, Symbol.GBPJPY },
    { Fx.Grpc.Symbol.GbpUsd, Symbol.GBPUSD },
    { Fx.Grpc.Symbol.UsdJpy, Symbol.USDJPY }
};

Console.WriteLine("Terminal simulator...");

#region MyRegion
// 1 hour empty
//startTime = new DateTime(2023, 1, 1, 0, 0, 0);
//endTime = new DateTime(2023, 1, 1, 0, 0, 0);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 1 hour full
//endTime = new DateTime(2023, 6, 30, 0, 0, 0);
//startTime = endTime;
//Console.Write(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours. -> ");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 1 day 
//endTime = new DateTime(2023, 6, 28, 23, 0, 0);
//startTime = endTime.AddDays(-1).AddHours(1);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 1st week
//startTime = new DateTime(2023, 1, 1, 0, 0, 0);
//endTime = new DateTime(2023, 1, 7, 23, 0, 0);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 2nd weeks
//startTime = new DateTime(2023, 1, 8, 0, 0, 0);
//endTime = new DateTime(2023, 1, 14, 23, 0, 0);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));


// Now
//startTime = DateTime.Now;
//endTime = startTime;
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 3 hours before now
//endTime = DateTime.Now;
//startTime = endTime.AddHours(-3);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));

// 1 week before now
//endTime = DateTime.Now;
//startTime = endTime.AddDays(-7).AddHours(1);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine(result.Count().ToString("##,##0"));


//this one will get:
//Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.
//startTime = new DateTime(2023, 1, 1, 0, 0, 0);
//endTime = startTime.AddDays(7).AddHours(-1);
//do
//{
//    Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//    result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//    Console.WriteLine(result.Count().ToString("##,##0"));
//    startTime = startTime.Add(new TimeSpan(7, 0, 0, 0));
//    endTime = startTime.AddDays(7).AddHours(-1);
//} while (endTime <= new DateTime(2023, 12, 31)); 
#endregion


////1) get last known time of data from db <-- todo:
//// 2) send request to get saved data
//endTime = DateTime.Now;
//startTime = endTime.AddDays(-1).AddHours(-0);
//Console.WriteLine(Math.Ceiling((endTime - startTime).TotalHours + 1).ToString(CultureInfo.InvariantCulture) + " hours.");
//result = await GetHistoricalDataAsync(startTime, endTime).ConfigureAwait(false);
//Console.WriteLine($"Historical:{result.Count():##,##0}");
////3) send request to get buffered data
result = await GetBufferedDataAsync().ConfigureAwait(false);
Console.WriteLine($"Buffered:{result.Count():##,##0}");
//4) send request to get live data
var (liveDataTask, liveChannel) = await GetLiveDataAsync(cancellationTokenSource.Token).ConfigureAwait(false);
Console.WriteLine("Press any key to cancel........");
Console.ReadKey();
cancellationTokenSource.Cancel();
await liveChannel.ShutdownAsync().ConfigureAwait(false);


Console.WriteLine("End of the program. Press any key to exit ...");
Console.ReadKey();
return 1;

//async Task<IEnumerable<Quotation>> GetHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
//{
//    var difference = Math.Ceiling((endDateTimeInclusive - startDateTimeInclusive).TotalHours);
//    switch (difference)
//    {
//        case > maxHoursInCache: throw new InvalidOperationException($"Requested hours exceed maximum cache size of {maxHoursInCache} hours.");
//        case < 0: throw new InvalidOperationException("Start date cannot be later than end date.");
//    }

//    var tasks = new List<Task>();
//    var quotations = new List<Quotation>();
//    var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
//    var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour).AddHours(1);
//    currentHoursKey = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
//    var key = start;
//    do
//    {
//        if (!hoursCache.ContainsKey(key) || currentHoursKey.Equals(key))
//        {
//            tasks.Add(LoadHistoricalDataAsync(key, key));
//        }

//        key = key.Add(new TimeSpan(1, 0, 0));
//    }
//    while (key < end);

//    await Task.WhenAll(tasks).ConfigureAwait(false);

//    key = start;
//    do
//    {
//        if (hoursCache.ContainsKey(key))
//        {
//            quotations.AddRange(GetData(key));
//        }
//        else
//        {
//            throw new NotImplementedException("The key is absent.");
//        }

//        key = key.Add(new TimeSpan(1, 0, 0));
//    }
//    while (key < end);
//    return quotations;
//}
//async Task LoadHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
//{
//    if (!startDateTimeInclusive.Equals(endDateTimeInclusive))
//    {
//        throw new InvalidOperationException("Start and end times must be the same. This function only supports processing of a single hour at a time.");
//    }

//    try
//    {
//        var quotations = await GetDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
//        ProcessData(startDateTimeInclusive, quotations);
//    }
//    catch (RpcException rpcException)
//    {
//        Console.WriteLine(rpcException);
//        throw;
//    }
//    catch (SocketException socketException)
//    {
//        Console.WriteLine(socketException);
//        throw;
//    }

//    catch (Exception exception)
//    {
//        Console.WriteLine(exception);
//        throw;
//    }
//}
//async Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
//{
//    var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = maxSendMessageSize, MaxReceiveMessageSize = maxReceiveMessageSize };
//    using var channel = GrpcChannel.ForAddress(grpcChannelAddress, channelOptions);
//    var client = new DataProvider.DataProviderClient(channel);
//    var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(deadline)));
//    var request = new DataRequest
//    {
//        StartDateTime = Timestamp.FromDateTime(startDateTimeInclusive.ToUniversalTime()),
//        Code = DataRequest.Types.StatusCode.HistoricalData
//    };

//    try
//    {
//        var call = client.GetDataAsync(callOptions);
//        await call.RequestStream.WriteAsync(request).ConfigureAwait(false);
//        await call.RequestStream.CompleteAsync().ConfigureAwait(false);

//        int counter = default;
//        IList<Quotation> quotations = new List<Quotation>();

//        await foreach (var response in call.ResponseStream.ReadAllAsync())
//        {
//            switch (response.Status.Code)
//            {
//                case DataResponseStatus.Types.StatusCode.Ok:
//                    foreach (var item in response.Quotations)
//                    {
//                        var quotation = new Quotation(counter++, ToEntitiesSymbol(item.Symbol), item.Datetime.ToDateTime().ToUniversalTime(), item.Ask, item.Bid);
//                        quotations.Add(quotation);
//                    }
//                    break;
//                case DataResponseStatus.Types.StatusCode.NoData:
//                    break;
//                case DataResponseStatus.Types.StatusCode.ServerError:
//                    throw new Exception($"Server error: {response.Status.Details}");
//                default:
//                    throw new Exception($"Unknown status code: {response.Status.Code}");
//            }
//        }

//        return quotations;
//    }
//    catch (RpcException rpcException)
//    {
//        Console.WriteLine(rpcException);
//        throw;
//    }
//    catch (Exception exception)
//    {
//        Console.WriteLine(exception);
//        throw;
//    }
//}
//void ProcessData(DateTime dateTime, IEnumerable<Quotation> quotations)
//{
//    var done = false;
//    var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.DateTime.Year });
//    foreach (var yearGroup in groupedByYear)
//    {
//        var year = yearGroup.Key.Year;
//        var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.DateTime.Month });
//        foreach (var monthGroup in groupedByMonth)
//        {
//            var month = monthGroup.Key.Month;
//            var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.DateTime.Day });
//            foreach (var dayGroup in groupedByDay)
//            {
//                var day = dayGroup.Key.Day;
//                var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.DateTime.Hour });
//                foreach (var hourGroup in groupedByHour)
//                {
//                    var hour = hourGroup.Key.Hour;
//                    var key = new DateTime(year, month, day, hour, 0, 0);
//                    var quotationsToSave = hourGroup.ToList();
//                    SetData(key, quotationsToSave);
//                    done = true;
//                    Console.WriteLine($"year:{year}, month:{month:D2}, day:{day:D2}, hour:{hour:D2}. Count:{quotationsToSave.Count}");
//                }
//            }
//        }
//    }

//    if (!done)
//    {
//        SetData(dateTime, quotations: new List<Quotation>());
//    }
//}
//void AddData(DateTime key, List<Quotation> quotations)
//{
//    if (hoursCache.Count >= maxHoursInCache)
//    {
//        if (hoursKeys.TryDequeue(out var oldestKey))
//        {
//            hoursCache.TryRemove(oldestKey, out _);
//        }
//    }

//    if (!hoursCache.ContainsKey(key))
//    {
//        hoursCache[key] = quotations;
//        hoursKeys.Enqueue(key);
//    }
//    else
//    {
//        if (!currentHoursKey.Equals(key))
//        {
//            throw new InvalidOperationException("the key already exists.");
//        }

//        hoursCache[key] = quotations;
//    }
//}
//void SetData(DateTime key, List<Quotation> quotations)
//{
//    AddData(key, quotations);
//}
//IEnumerable<Quotation> GetData(DateTime key)
//{
//    hoursCache.TryGetValue(key, out var quotations);
//    return quotations!;
//}
Symbol ToEntitiesSymbol(Fx.Grpc.Symbol protoSymbol)
{
    if (!symbolMapping!.TryGetValue(protoSymbol, out var symbol))
    {
        throw new ArgumentOutOfRangeException(nameof(protoSymbol), protoSymbol, null);
    }

    return symbol;
}








async Task<IEnumerable<Quotation>> GetBufferedDataAsync()
{
    var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = maxSendMessageSize, MaxReceiveMessageSize = maxReceiveMessageSize };
    using var channel = GrpcChannel.ForAddress(grpcChannelAddress, channelOptions);
    var client = new DataProvider.DataProviderClient(channel);
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(deadline)));
    var request = new DataRequest
    {
        Code = DataRequest.Types.StatusCode.BufferedData
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
        Console.WriteLine(rpcException);
        throw;
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception);
        throw;
    }
}


async Task<(Task, GrpcChannel)> GetLiveDataAsync(CancellationToken token)
{
    var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = maxSendMessageSize, MaxReceiveMessageSize = maxReceiveMessageSize };
    var channel = GrpcChannel.ForAddress(grpcChannelAddress, channelOptions);
    var client = new DataProvider.DataProviderClient(channel);
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(deadline)));
    var request = new DataRequest
    {
        Code = DataRequest.Types.StatusCode.LiveData
    };

    var call = client.GetDataAsync(callOptions);
    await call.RequestStream.WriteAsync(request, token).ConfigureAwait(false);
    await call.RequestStream.CompleteAsync().ConfigureAwait(false);
    
    _ = Task.Run(async () =>
    {
        try
        {
            int counter = default;
            await foreach (var response in call.ResponseStream.ReadAllAsync().WithCancellation(token))
            {
                switch (response.Status.Code)
                {
                    case DataResponseStatus.Types.StatusCode.Ok:
                        foreach (var item in response.Quotations)
                        {
                            var quotation = new Quotation(counter++, ToEntitiesSymbol(item.Symbol), item.Datetime.ToDateTime().ToUniversalTime(), item.Ask, item.Bid);
                            liveDataQueue.Add(quotation, token);
                        }
                        break;
                    case DataResponseStatus.Types.StatusCode.NoData:
                    case DataResponseStatus.Types.StatusCode.ServerError: liveDataQueue.CompleteAdding(); break;
                    default: throw new Exception($"Unknown status code: {response.Status.Code}");
                }
            }
        }
        catch (OperationCanceledException operationCanceledException)
        {
            Console.WriteLine(operationCanceledException.Message);
        }
        catch (RpcException rpcException)
        {
            Console.WriteLine(rpcException.Message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            liveDataQueue.CompleteAdding();
        }
    }, token);

    var processingTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var quotation in liveDataQueue.GetConsumingAsyncEnumerable(token).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                Console.WriteLine(quotation.ToString());
            }
        }
        catch (OperationCanceledException operationCanceledException)
        {
            Console.WriteLine($"{operationCanceledException.Message}");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            throw;
        }
    }, token);

    return (processingTask, channel);
}
