/*+------------------------------------------------------------------+
  |                   MetaQuotes.Prototype.Indicator.PipeMethodCalls |
  |                                                        Client.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Entities;
using Common.MetaQuotes.Mediator;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace MetaQuotes.Client.IndicatorToMediator;

public class Client : IDisposable
{
    private const string ok = "ok";
    private const string noConnection = "Unable to establish a connection with the server.";
    private readonly PipeClient<IQuotationsMessenger> pipeClient;
    private bool connected;

    private readonly BlockingCollection<(int symbol, string datetime, double ask, double bid)> Quotations = new();
    private readonly object ResultsLock = new();
    private readonly Queue<string> Results = new();

    private readonly CancellationTokenSource cts = new();
    public event Action OnInitializationComplete;
    private readonly Action processQuotationsAction;

    public Client(Symbol symbol, bool enableLogging = false)
    {
        pipeClient = new PipeClient<IQuotationsMessenger>(new NetJsonPipeSerializer(), $"IndicatorToMediator_{symbol}");
        if (enableLogging) pipeClient.SetLogger(Console.WriteLine);

        processQuotationsAction = () => Task.Run(() => ProcessQuotationsAsync(cts.Token), cts.Token).ConfigureAwait(false);
        OnInitializationComplete += processQuotationsAction;
    }

    public void Dispose()
    {
        OnInitializationComplete -= processQuotationsAction;
        cts.Cancel();
        pipeClient.Dispose();
    }

    internal async Task DeInitAsync(Symbol symbol, DeInitReason reason)
    {
        try
        {
            await pipeClient.InvokeAsync(x => x.DeInit(symbol, reason)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    internal async Task<string> InitAsync(int symbol, string datetime, double ask, double bid, int environment)
    {
        connected = await InitializeClientAsync().ConfigureAwait(false);
        try
        {
            if (!connected) return noConnection;
            var result = await pipeClient.InvokeAsync(x => x.Init(symbol, datetime, ask, bid, environment)).ConfigureAwait(false);
            if (result == ok) OnInitializationComplete?.Invoke(); 
            return result;
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

    public string Add(int symbol, string datetime, double ask, double bid)
    {
        Quotations.Add((symbol, datetime, ask, bid));
        lock (ResultsLock) if (Results.Count > 0) { return Results.Dequeue(); }
        return ok;
    }

    private async Task<bool> InitializeClientAsync()
    {
        const int maxAttempts = 5;
        const int retryDelayMilliseconds = 1000;
        const int connectionTimeoutMilliseconds = 1000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                var cts = new CancellationTokenSource(connectionTimeoutMilliseconds);
                await pipeClient.ConnectAsync(cts.Token).ConfigureAwait(false);
                return true;
            }
            catch
            {
                await Task.Delay(retryDelayMilliseconds).ConfigureAwait(false);
            }

        return false;
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

    private async Task ProcessQuotationsAsync(CancellationToken ct)
    {
        foreach (var (symbol, datetime, ask, bid) in Quotations.GetConsumingEnumerable())
        {
            if (ct.IsCancellationRequested) break;
            var result = await TickAsync((symbol, datetime, ask, bid)).ConfigureAwait(false);
            lock (ResultsLock) Results.Enqueue(result);
        }
    }

    private async Task<string> TickAsync((int symbol, string datetime, double ask, double bid) quotation)
    {
        try
        {
            if (!connected) return noConnection;
            return await pipeClient.InvokeAsync(x => x.Add(quotation.symbol, quotation.datetime, quotation.ask, quotation.bid)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

   
}