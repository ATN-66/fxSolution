using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.MetaQuotes.Mediator;
using PipeMethodCalls;
using PipeMethodCalls.NetJson;

namespace MetaQuotes.Executive;

public class ExecutiveClient : IDisposable
{
    private readonly Guid guid = Guid.NewGuid();
    private const string ok = "ok";
    private const string noConnection = "Unable to establish a connection with the server.";
    private readonly PipeClient<IExecutiveMessenger> pipeClient;
    private bool connected;

    private readonly BlockingCollection<(string dateTime, int type, int code, string message)> messages = new();

    private readonly CancellationTokenSource cts = new();
    private event Action OnInitializationComplete;
    private readonly Action processQuotationsAction;

    internal ExecutiveClient(bool enableLogging = false)
    {
        pipeClient = new PipeClient<IExecutiveMessenger>(new NetJsonPipeSerializer(), $"EXECUTIVE");
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

    internal void DeInit(string dateTime)
    {
        try
        {
            if (!messages.IsAddingCompleted) messages.CompleteAdding();
            while (!messages.IsCompleted) Task.Delay(1000).ConfigureAwait(false).GetAwaiter();
            pipeClient.InvokeAsync(x => x.DeInit(dateTime)).ConfigureAwait(false).GetAwaiter();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    internal async Task<string> InitAsync(string datetime)
    {
        connected = await InitializeClientAsync().ConfigureAwait(false);
        try
        {
            if (!connected) return noConnection;
            var result = await pipeClient.InvokeAsync(x => x.InitAsync(datetime)).ConfigureAwait(false);
            if (result == ok) OnInitializationComplete?.Invoke();
            return $"EXECUTIVE:{guid}:{ok}";
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

    public string Pulse(string dateTime, int type, int code, string message)
    {
        messages.Add((dateTime, type, code, message));
        return ok;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var (dateTime, type, code, message) in messages.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            if (ct.IsCancellationRequested) break;
            await pipeClient.InvokeAsync(x => x.Pulse(dateTime, type, code, message), cancellationToken: ct).ConfigureAwait(false);
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