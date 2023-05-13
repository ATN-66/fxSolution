/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                           QuotationsProcessor.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Timers;
using Common.Entities;
using Mediator.Client.Mediator.To.Terminal;
using Mediator.Repository;
using Protos.Grpc;
using Environment = Common.Entities.Environment;
using Timer = System.Timers.Timer;

namespace Mediator.Processors;

public class QuotationsProcessor
{
    private readonly string[] _formats = { "yyyy.MM.dd HH:mm:ss" };

    private readonly Administrator.Administrator _administrator;
    private readonly MediatorToTerminalClient _mediatorToTerminalClient;
    private readonly IMSSQLRepository _repository;

    private readonly Quotation[] _lastKnownQuotations;

    private const string ok = "ok";
    private readonly Timer _saveTimer;
    private const int _batchSize = 1000;
    private const int minutes = 10;
    private readonly ReaderWriterLockSlim _queueLock = new();
    private readonly object lockObject = new();
    private readonly BlockingCollection<(int symbol, string datetime, double ask, double bid)> _quotations = new();
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();

    public QuotationsProcessor(Administrator.Administrator administrator,
        MediatorToTerminalClient mediatorToTerminalClient, IMSSQLRepository repository)
    {
        _administrator = administrator;
        _mediatorToTerminalClient = mediatorToTerminalClient;
        _repository = repository;

        _lastKnownQuotations = new Quotation[_administrator.TotalIndicators];

        _saveTimer = new Timer(minutes * 60 * 1000);
        _saveTimer.Elapsed += OnSaveTimerElapsedAsync;
        _saveTimer.AutoReset = true;
        _saveTimer.Enabled = true;
    }

    public void DeInit(int symbol, int reason)
    {
        lock (lockObject)
        {
            Console.Write(".");
            _lastKnownQuotations[symbol - 1] = Quotation.Empty;
            _administrator.Environments[symbol - 1] = null;
            _administrator.ConnectedIndicators[symbol - 1] = false;
            _administrator.DeInitReasons[symbol - 1] = (DeInitReason)reason;
            if (_administrator.ConnectedIndicators.Any(connection => connection)) return;
            Console.WriteLine($"The indicators were disconnected.");
        }
    }

    public string Init(int symbol, string datetime, double ask, double bid, int environment)
    {
        lock (lockObject)
        {
            if (_lastKnownQuotations[symbol - 1] == Quotation.Empty)
            {
                Debug.Assert(_administrator.Environments[symbol - 1] == null);
                _administrator.Environments[symbol - 1] = (Environment)environment;

                Debug.Assert(_administrator.ConnectedIndicators[symbol - 1] == false);
                _administrator.ConnectedIndicators[symbol - 1] = true;

                var resultSymbol = (Symbol)symbol;
                var resultDateTime = DateTime
                    .ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();
                var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
                var resultBid = Normalize(resultSymbol, Side.Bid, bid);
                var quotation = new Quotation(resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);
                _lastKnownQuotations[symbol - 1] = quotation;
                _quotationsToSave.Enqueue(quotation); //todo: to add
            }
            else throw new Exception(Administrator.Administrator.MultipleConnections);

            if (_administrator.IndicatorsConnected)
                Console.WriteLine($".The indicators were connected. Environment is {_administrator.Environment}");
            else Console.Write(".");
        }

        return ok;
    }

    public string Tick(int symbol, string datetime, double ask, double bid)
    {
        _quotations.Add((symbol, datetime, ask, bid));
        //lock (ResultsLock) if (Results.Count > 0) { return Results.Dequeue(); }
        return ok;
    }

    private async Task<string> TickAsync(int symbol, string datetime, double ask, double bid)
    {
        var resultSymbol = (Symbol)symbol;
        var resultDateTime = DateTime.ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToUniversalTime();
        while (_lastKnownQuotations[symbol - 1].DateTime >= resultDateTime)
            resultDateTime = resultDateTime.AddMilliseconds(1);
        var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
        var resultBid = Normalize(resultSymbol, Side.Bid, bid);
        var quotation = new Quotation(resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);

        if (quotation.IntAsk != _lastKnownQuotations[symbol - 1].IntAsk ||
            quotation.IntBid != _lastKnownQuotations[symbol - 1].IntBid)
        {
            _mediatorToTerminalClient.Tick(quotation); //todo: async
        }

        bool shouldSave;
        _queueLock.EnterWriteLock();
        try
        {
            _lastKnownQuotations[symbol - 1] = quotation;
            _quotationsToSave.Enqueue(quotation);
            shouldSave = _quotationsToSave.Count >= _batchSize;
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }

        if (shouldSave) await SaveQuotationsAsync().ConfigureAwait(false);

        return ok;
    }

    private void OnSaveTimerElapsedAsync(object? sender, ElapsedEventArgs e)
    {
        SaveQuotationsAsync().ConfigureAwait(false);
    }

    private async Task SaveQuotationsAsync()
    {
        var quotationsToSave = new List<Quotation>();

        _queueLock.EnterReadLock();
        try
        {
            while (_quotationsToSave.TryDequeue(out var quotation)) quotationsToSave.Add(quotation);
        }
        finally
        {
            _queueLock.ExitReadLock();
        }

        if (quotationsToSave.Count == 0) return;
        await _repository.SaveQuotationsAsync(quotationsToSave).ConfigureAwait(false);
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