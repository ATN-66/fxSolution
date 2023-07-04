﻿/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.DataSource;
using Common.Entities;
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

public class Mediator : DataSource, IMediator
{
    private const int Deadline = int.MaxValue;
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

    public Mediator(IConfiguration configuration, ILogger<IMediator> logger, IAudioPlayer audioPlayer) : base(configuration, logger, audioPlayer)
    {
        _grpcChannelAddress = configuration.GetValue<string>($"{nameof(_grpcChannelAddress)}")!;
        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
    }

    public async Task<(Task, GrpcChannel)> StartAsync(BlockingCollection<Quotation> quotations, CancellationToken token)
    {
        var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = _maxSendMessageSize, MaxReceiveMessageSize = _maxReceiveMessageSize };
        var channel = GrpcChannel.ForAddress(_grpcChannelAddress, channelOptions);
        var client = new DataProvider.DataProviderClient(channel);
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(Deadline)));
        var request = new DataRequest
        {
            Code = DataRequest.Types.StatusCode.LiveData
        };

        var call = client.GetDataAsync(callOptions);
        await call.RequestStream.WriteAsync(request, token).ConfigureAwait(false);
        await call.RequestStream.CompleteAsync().ConfigureAwait(false);

        var receivingTask = Task.Run(async () =>
        {
            try
            {
                int counter = default;
                await foreach (var response in call.ResponseStream.ReadAllAsync(token).WithCancellation(token))
                {
                    switch (response.Status.Code)
                    {
                        case DataResponseStatus.Types.StatusCode.Ok:
                            foreach (var item in response.Quotations)
                            {
                                var quotation = new Quotation(counter++, ToEntitiesSymbol(item.Symbol), item.Datetime.ToDateTime().ToUniversalTime(), item.Ask, item.Bid);
                                quotations.Add(quotation, token);
                            }
                            break;
                        case DataResponseStatus.Types.StatusCode.NoData:
                        case DataResponseStatus.Types.StatusCode.ServerError: quotations.CompleteAdding(); break;
                        default: throw new Exception($"Unknown status code: {response.Status.Code}");
                    }
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                if (!token.IsCancellationRequested)
                {
                    LogException(operationCanceledException, "");
                    throw;
                }
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode == StatusCode.Cancelled)
                {
                }
                else
                {
                    LogException(rpcException, "");
                    throw;
                }
            }
            catch (Exception exception)
            {
                LogException(exception, "");
                throw;
            }
            finally
            {
                quotations.CompleteAdding();
            }
        }, token);

        return (receivingTask, channel);
    }
    protected override Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, CancellationToken token)
    {
        var request = new DataRequest
        {
            StartDateTime = Timestamp.FromDateTime(startDateTimeInclusive.ToUniversalTime()),
            Code = DataRequest.Types.StatusCode.HistoricalData
        };

        return GetStaticAsync(request, token);
    }
    public Task<IList<Quotation>> GetBufferedDataAsync(CancellationToken token)
    {
        var request = new DataRequest
        {
            Code = DataRequest.Types.StatusCode.BufferedData
        };

        return GetStaticAsync(request, token);
    }
    private async Task<IList<Quotation>> GetStaticAsync(DataRequest request, CancellationToken token)
    {
        var channelOptions = new GrpcChannelOptions { MaxSendMessageSize = _maxSendMessageSize, MaxReceiveMessageSize = _maxReceiveMessageSize };
        using var channel = GrpcChannel.ForAddress(_grpcChannelAddress, channelOptions);
        var client = new DataProvider.DataProviderClient(channel);
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(Deadline)));

        try
        {
            var call = client.GetDataAsync(callOptions);
            await call.RequestStream.WriteAsync(request, token).ConfigureAwait(false);
            await call.RequestStream.CompleteAsync().ConfigureAwait(false);

            int counter = default;
            IList<Quotation> quotations = new List<Quotation>();

            await foreach (var response in call.ResponseStream.ReadAllAsync(token).WithCancellation(token))
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
                    case DataResponseStatus.Types.StatusCode.NoData: break;
                    case DataResponseStatus.Types.StatusCode.ServerError: throw new Exception($"Server error: {response.Status.Details}");
                    default: throw new Exception($"Unknown status code: {response.Status.Code}");
                }
            }

            return quotations;
        }
        catch (RpcException rpcException)
        {
            LogException(rpcException, "");
            throw;
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }
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