/*+------------------------------------------------------------------+
  |                            MetaQuotes.Client.IndicatorToMediator |
  |                                                        Client.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Entities;
using Common.MetaQuotes.Mediator;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace MetaQuotes.Client.IndicatorToMediator;

internal sealed class Client : IDisposable
{
    private const string ok = "ok";
    private const string noConnection = "Unable to establish a connection with the server.";
    private readonly PipeClient<IQuotationsMessenger> pipeClient;
    private bool connected;

    private readonly BlockingCollection<(int id, int symbol, string datetime, double ask, double bid)> quotations = new();

    private readonly CancellationTokenSource cts = new();
    private event Action OnInitializationComplete;
    private readonly Action processQuotationsAction;

    internal Client(Symbol symbol, bool enableLogging = false)
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

    internal async Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        connected = await InitializeClientAsync().ConfigureAwait(false);
        try
        {
            if (!connected) return noConnection;
            var result = await pipeClient.InvokeAsync(x => x.Init(id, symbol, datetime, ask, bid, environment)).ConfigureAwait(false);
            if (result == ok) OnInitializationComplete?.Invoke(); 
            return result;
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

    internal string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        quotations.Add((id, symbol, datetime, ask, bid));
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
        foreach (var (id, symbol, datetime, ask, bid) in quotations.GetConsumingEnumerable())
        {
            if (ct.IsCancellationRequested) break;
            await pipeClient.InvokeAsync(x => x.Tick(id, symbol, datetime, ask, bid), cancellationToken: ct).ConfigureAwait(false);
        }
    }
}