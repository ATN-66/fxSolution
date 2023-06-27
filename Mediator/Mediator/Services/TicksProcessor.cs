/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                                TicksProcessor.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using System.Collections.Concurrent;
using System.Globalization;
using Mediator.Contracts.Services;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using Mediator.ViewModels;

namespace Mediator.Services;

internal class TicksProcessor : ITicksProcessor
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly MainViewModel _mainViewModel;
    private readonly IDataService _dataService;

    private readonly SemaphoreSlim _mutex = new(1);
    private readonly ReaderWriterLockSlim _queueLock = new();

    private readonly Quotation[] _lastKnownQuotations = new Quotation[TotalIndicators];
    private BlockingCollection<(int id, int symbol, string datetime, double ask, double bid)> _quotations = new();
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();
    private int[] _counter = new int[TotalIndicators];

    private const string Ok = "ok";
    private readonly string _format;
    
    private const int BatchSize = 1000;
    private const int Minutes = 10;
    private readonly Timer _saveTimer;

    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    //private readonly Client.Mediator.To.Terminal.Client _client;

    public TicksProcessor(IConfiguration configuration, MainViewModel mainViewModel, CancellationTokenSource cts, IDataService dataService, ILogger<TicksProcessor> logger)
    {
        _mainViewModel = mainViewModel;
        _dataService = dataService;
        //this._client = client;

        _format = configuration.GetValue<string>("MT5DateTimeFormat")!;

        _saveTimer = new Timer(Minutes * 60 * 1000);
        _saveTimer.Elapsed += OnSaveTimerElapsedAsync;
        _saveTimer.AutoReset = true;
        _saveTimer.Enabled = true;

        void ProcessQuotationsAction() => Task.Run(() => ProcessAsync(cts.Token), cts.Token).ConfigureAwait(false);
        _mainViewModel.InitializationComplete += ProcessQuotationsAction;

        logger.LogTrace("({_guid}) is ON.", _guid);
    }
    
    public async Task DeInitAsync(int reason)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_mainViewModel.IndicatorsConnected)
            {
                return;
            }

            if (!_quotations.IsAddingCompleted)
            {
                _quotations.CompleteAdding();
            }

            while (!_quotations.IsCompleted)
            {
                Task.Delay(1000).ConfigureAwait(false).GetAwaiter();
            }

            await SaveQuotationsAsync().ConfigureAwait(false);

            for (var i = 0; i < TotalIndicators; i++)
            {
                _lastKnownQuotations[i] = Quotation.Empty;
            }

            await _mainViewModel.IndicatorDisconnectedAsync((DeInitReason)reason, _counter.Sum()).ConfigureAwait(false);
            _quotations = new();
            _counter = new int[TotalIndicators];
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int workplace)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_mainViewModel.IndicatorsConnected)
            {
                throw new NotImplementedException();

                //_mainViewModel.CloseApplicationOnException("Unable to connect the indicator. Maximum capacity of indicators has been reached.");
            }

            if (_lastKnownQuotations[symbol - 1] == Quotation.Empty)
            {
                await _mainViewModel.IndicatorConnectedAsync((Symbol)symbol, (Workplace)workplace).ConfigureAwait(false);
                var resultSymbol = (Symbol)symbol;
                var resultDateTime = DateTime.ParseExact(datetime, _format, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
                var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid);
                _lastKnownQuotations[symbol - 1] = quotation;
                _quotations.Add((id, symbol, datetime, ask, bid));
                _mainViewModel.SetIndicator(resultSymbol, resultDateTime, ask, bid, 0);
            }
            else
            {
                throw new NotImplementedException();

                //_mainViewModel.CloseApplicationOnException("Connection failed. Each indicator can only be connected once.");
            }
        }
        finally
        {
            _mutex.Release();
        }

        return Ok;
    }

    public string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        _quotations.Add((id, symbol, datetime, ask, bid));
        return Ok;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var (id, symbol, datetime, ask, bid) in _quotations.GetConsumingAsyncEnumerable(ct).WithCancellation(ct))
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }
            Process(id, symbol, datetime, ask, bid);
        }
    }

    private void Process(int id, int symbol, string datetime, double ask, double bid)
    {
        var resultSymbol = (Symbol)symbol;
        var resultDateTime = DateTime.ParseExact(datetime, _format, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        while (_lastKnownQuotations[symbol - 1].DateTime >= resultDateTime)
        {
            resultDateTime = resultDateTime.AddMilliseconds(1);
        }
        _counter[symbol - 1] += 1;
        //_client.Tick(quotation); //todo: 
        var quotation = new Quotation(id, resultSymbol, resultDateTime, ask, bid);
        _mainViewModel.SetIndicator(resultSymbol, resultDateTime, ask, bid, _counter[symbol - 1]);
        
        bool shouldSave;
        _queueLock.EnterWriteLock();
        try
        {
            _lastKnownQuotations[symbol - 1] = quotation;
            _quotationsToSave.Enqueue(quotation);
            shouldSave = _quotationsToSave.Count >= BatchSize;
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }

        if (shouldSave)
        {
            SaveQuotationsAsync().ConfigureAwait(false);
        }
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
            while (_quotationsToSave.TryDequeue(out var quotation))
            {
                quotationsToSave.Add(quotation);
            }
        }
        finally
        {
            _queueLock.ExitReadLock();
        }

        if (quotationsToSave.Count == 0)
        {
            return;
        }

        await _dataService.SaveQuotationsAsync(quotationsToSave).ConfigureAwait(false);
    }
}