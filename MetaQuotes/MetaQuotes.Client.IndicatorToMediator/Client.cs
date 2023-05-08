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
using Environment = Common.Entities.Environment;

namespace MetaQuotes.Client.IndicatorToMediator;

public class Client : IDisposable
{
    private const string noConnection = "Unable to establish a connection with the server.";
    private readonly PipeClient<IQuotationsMessenger> pipeClient;
    private bool connected;
    //private readonly BlockingCollection<Quotation> Quotations = new();
    //private static readonly object ResultsLock = new();
    //private readonly Queue<string> Results = new();

    public Client(Symbol symbol)
    {
        pipeClient = new PipeClient<IQuotationsMessenger>(new NetJsonPipeSerializer(), $"IndicatorToMediator_{symbol}");
        //Task.Run(ProcessQuotationsAsync);
    }

    public bool EnableLogging { get; set; }

    public void Dispose()
    {
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
    
    internal async Task<string> InitAsync(int symbol, string datetime, int ask, int bid, int environment)
    {
        connected = await InitializeClientAsync().ConfigureAwait(false);
        try
        {
            if (!connected) return noConnection;
            return await pipeClient.InvokeAsync(x => x.Init(symbol, datetime, ask, bid, environment)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

    private async Task<string> TickAsync(int symbol, string datetime, int ask, int bid)
    {
        try
        {
            if (!connected) return noConnection;
            return await pipeClient.InvokeAsync(x => x.Tick(symbol, datetime, ask, bid)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            connected = false;
            return ExceptionDetails(ex);
        }
    }

    private async Task<bool> InitializeClientAsync()
    {
        const int maxAttempts = 10;
        const int retryDelayMilliseconds = 1000;
        const int connectionTimeoutMilliseconds = 5000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                var cts = new CancellationTokenSource(connectionTimeoutMilliseconds);
                await pipeClient.ConnectAsync(cts.Token).ConfigureAwait(false);
                if (EnableLogging) pipeClient.SetLogger(Console.WriteLine);
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

    //private async Task ProcessQuotationsAsync()
    //{
    //    foreach (var quotation in Quotations.GetConsumingEnumerable())
    //    {
    //        var result = await TickAsync(quotation).ConfigureAwait(false);
    //        lock (ResultsLock) Results.Enqueue(result);
    //    }
    //}

    //public string AddQuotationToQueue(Quotation quotation)
    //{
    //    Quotations.Add(quotation);

    //    lock (ResultsLock)
    //    {
    //        if (Results.Count > 0) { return Results.Dequeue(); }
    //    }

    //    return "ok";
    //}
}