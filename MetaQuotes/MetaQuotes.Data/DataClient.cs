﻿/*+------------------------------------------------------------------+
  |                                                  MetaQuotes.Data |
  |                                                    DataClient.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.MetaQuotes.Mediator;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace MetaQuotes.Data;

internal sealed class DataClient : IDisposable
{
    private readonly Guid guid = Guid.NewGuid();
    private const string ok = "ok";
    private const string noConnection = "Unable to establish a connection with the server.";
    private readonly PipeClient<IDataMessenger> pipeClient;
    private bool connected;

    private readonly BlockingCollection<(int symbol, string datetime, double ask, double bid)> quotations = new();

    private readonly CancellationTokenSource cts = new();
    private event Action OnInitializationComplete;
    private readonly Action processQuotationsAction;

    internal DataClient(int symbol, bool enableLogging = false)
    {
        pipeClient = new PipeClient<IDataMessenger>(new NetJsonPipeSerializer(), $"DATA.{symbol}");
        if (enableLogging) pipeClient.SetLogger(Console.WriteLine);

        processQuotationsAction = () => Task.Run(() => ProcessAsync(cts.Token), cts.Token).ConfigureAwait(false);
        OnInitializationComplete += processQuotationsAction;
    }

    public void Dispose()
    {
        OnInitializationComplete -= processQuotationsAction;
        cts.Cancel();
        pipeClient.Dispose();
    }

    internal void DeInit(int symbol, int reason)
    {
        try
        {
            if (!quotations.IsAddingCompleted) quotations.CompleteAdding();
            while (!quotations.IsCompleted) Task.Delay(1000).ConfigureAwait(false).GetAwaiter();
            pipeClient.InvokeAsync(x => x.DeInit(symbol, reason)).ConfigureAwait(false).GetAwaiter();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    internal async Task<string> InitAsync(int symbol, string datetime, double ask, double bid, int workplace)
    {
        connected = await InitializeClientAsync().ConfigureAwait(false);
        try
        {
            if (!connected) return noConnection;
            var result = await pipeClient.InvokeAsync(x => x.InitAsync(symbol, datetime, ask, bid, workplace)).ConfigureAwait(false);
            if (result == ok) OnInitializationComplete?.Invoke(); 
            return $"{symbol}:{guid}:{ok}";
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

    private async Task<bool> InitializeClientAsync()
    {
        const int maxAttempts = 5;
        const int retryDelayMilliseconds = 1000;
        const int connectionTimeoutMilliseconds = 1000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(connectionTimeoutMilliseconds);
                await pipeClient.ConnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                return true;
            }
            catch
            {
                await Task.Delay(retryDelayMilliseconds).ConfigureAwait(false);
            }

        return false;
    }

    internal string Tick(int symbol, string datetime, double ask, double bid)
    {
        quotations.Add((symbol, datetime, ask, bid));
        return ok;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var (symbol, datetime, ask, bid) in quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            if (ct.IsCancellationRequested) break;
            await pipeClient.InvokeAsync(x => x.Tick(symbol, datetime, ask, bid), cancellationToken: ct).ConfigureAwait(false);
        }
    }

    private static string ExceptionDetails(Exception ex)
    {
        var exceptionDetails = new StringBuilder();
        var currentEx = ex;

        while (currentEx != null)
        {
            exceptionDetails.AppendLine($"Exception Type: {currentEx.GetType().FullName}");
            exceptionDetails.AppendLine($"Message: {currentEx.Message}");
            exceptionDetails.AppendLine($"Stack Trace: {currentEx.StackTrace}");

            currentEx = currentEx.InnerException;

            if (currentEx != null) exceptionDetails.AppendLine("Inner Exception:");
        }

        return exceptionDetails.ToString();
    }
}