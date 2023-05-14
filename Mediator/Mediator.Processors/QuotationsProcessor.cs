/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                           QuotationsProcessor.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Timers;
using Common.Entities;
using Mediator.Client.Mediator.To.Terminal;
using Mediator.Repository;
using Environment = Common.Entities.Environment;
using Timer = System.Timers.Timer;

namespace Mediator.Processors;

public sealed class QuotationsProcessor : IDisposable
{
    private const string ok = "ok";
    private const int batchSize = 1000;
    private const int minutes = 10;
    private readonly string[] _formats = { "yyyy.MM.dd HH:mm:ss" };
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();

    private readonly Administrator.Administrator administrator;

    private readonly CancellationTokenSource cts = new();

    private readonly Quotation[] lastKnownQuotations;
    private readonly object lockObject = new();
    private readonly MediatorToTerminalClient mediatorToTerminalClient;
    private readonly Action processQuotationsAction;
    private readonly ReaderWriterLockSlim queueLock = new();
    private readonly BlockingCollection<(int id, int symbol, string datetime, double ask, double bid)> quotations = new();
    private readonly IMSSQLRepository repository;
    private readonly Timer saveTimer;

    public QuotationsProcessor(Administrator.Administrator administrator,
        MediatorToTerminalClient mediatorToTerminalClient, IMSSQLRepository repository)
    {
        this.administrator = administrator;
        this.mediatorToTerminalClient = mediatorToTerminalClient;
        this.repository = repository;

        lastKnownQuotations = new Quotation[this.administrator.TotalIndicators];

        saveTimer = new Timer(minutes * 60 * 1000);
        saveTimer.Elapsed += OnSaveTimerElapsedAsync;
        saveTimer.AutoReset = true;
        saveTimer.Enabled = true;

        processQuotationsAction =
            () => Task.Run(() => ProcessAsync(cts.Token), cts.Token).ConfigureAwait(false);
        OnInitializationComplete += processQuotationsAction;
    }

    public void Dispose()
    {
        OnInitializationComplete -= processQuotationsAction;
        cts.Cancel();
    }

    private event Action OnInitializationComplete;

    public void DeInit(int symbol, int reason)
    {
        lock (lockObject)
        {
            Console.Write(".");
            lastKnownQuotations[symbol - 1] = Quotation.Empty;
            administrator.Environments[symbol - 1] = null;
            administrator.ConnectedIndicators[symbol - 1] = false;
            administrator.DeInitReasons[symbol - 1] = (DeInitReason)reason;
            if (administrator.ConnectedIndicators.Any(connection => connection)) return;
            Console.WriteLine("The indicators were disconnected.");
        }
    }

    public string Init(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        lock (lockObject)
        {
            if (lastKnownQuotations[symbol - 1] == Quotation.Empty)
            {
                Debug.Assert(administrator.Environments[symbol - 1] == null);
                administrator.Environments[symbol - 1] = (Environment)environment;

                Debug.Assert(administrator.ConnectedIndicators[symbol - 1] == false);
                administrator.ConnectedIndicators[symbol - 1] = true;

                var resultSymbol = (Symbol)symbol;
                var resultDateTime = DateTime
                    .ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();
                var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
                var resultBid = Normalize(resultSymbol, Side.Bid, bid);
                var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);
                lastKnownQuotations[symbol - 1] = quotation;
                mediatorToTerminalClient.Tick(quotation); //todo: async
                _quotationsToSave.Enqueue(quotation);
            }
            else
            {
                throw new Exception(Administrator.Administrator.MultipleConnections);
            }

            if (administrator.IndicatorsConnected)
            {
                OnInitializationComplete.Invoke();
                Console.WriteLine($".The indicators were connected. Environment is {administrator.Environment}");
            }
            else
            {
                Console.Write(".");
            }
        }

        return ok;
    }

    public string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        quotations.Add((id, symbol, datetime, ask, bid));
        return ok;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach(var (id,symbol, datetime, ask, bid) in quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            if (ct.IsCancellationRequested) break;
            Process(id, symbol, datetime, ask, bid);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    private void Process(int id, int symbol, string datetime, double ask, double bid)
    {
        var resultSymbol = (Symbol)symbol;
        var resultDateTime = DateTime.ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToUniversalTime();
        while (lastKnownQuotations[symbol - 1].DateTime >= resultDateTime)
            resultDateTime = resultDateTime.AddMilliseconds(1);
        var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
        var resultBid = Normalize(resultSymbol, Side.Bid, bid);
        var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);

        if (quotation.IntAsk != lastKnownQuotations[symbol - 1].IntAsk ||
            quotation.IntBid != lastKnownQuotations[symbol - 1].IntBid)
            mediatorToTerminalClient.Tick(quotation); //todo: async

        bool shouldSave;
        queueLock.EnterWriteLock();
        try
        {
            lastKnownQuotations[symbol - 1] = quotation;
            _quotationsToSave.Enqueue(quotation);
            shouldSave = _quotationsToSave.Count >= batchSize;
        }
        finally
        {
            queueLock.ExitWriteLock();
        }

        if (shouldSave) SaveQuotationsAsync().ConfigureAwait(false);
    }

    private void OnSaveTimerElapsedAsync(object? sender, ElapsedEventArgs e)
    {
        SaveQuotationsAsync().ConfigureAwait(false);
    }

    private async Task SaveQuotationsAsync()
    {
        var quotationsToSave = new List<Quotation>();

        queueLock.EnterReadLock();
        try
        {
            while (_quotationsToSave.TryDequeue(out var quotation)) quotationsToSave.Add(quotation);
        }
        finally
        {
            queueLock.ExitReadLock();
        }

        if (quotationsToSave.Count == 0) return;
        await repository.SaveQuotationsAsync(quotationsToSave).ConfigureAwait(false);
    }

    private static int Normalize(Symbol symbol, Side side, double input)
    {
        var multiplier = symbol switch
        {
            Symbol.EURGBP => 100_000,
            Symbol.EURUSD => 100_000,
            Symbol.GBPUSD => 100_000,
            Symbol.EURJPY => 1_000,
            Symbol.GBPJPY => 1_000,
            Symbol.USDJPY => 1_000,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol))
        };

        var multipliedInput = (int)(input * multiplier);
        var (Quotient, Remainder) = Math.DivRem(multipliedInput, 10);
        switch (side)
        {
            case Side.Ask:
                if (Remainder >= 4) Quotient += 1;
                break;
            case Side.Bid:
                if (Remainder >= 6) Quotient += 1;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(side));
        }

        return Quotient;
    }
}