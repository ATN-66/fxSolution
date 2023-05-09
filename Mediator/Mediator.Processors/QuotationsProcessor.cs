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
using Environment = Common.Entities.Environment;
using Timer = System.Timers.Timer;

namespace Mediator.Processors;

public class QuotationsProcessor
{
    readonly string[] _formats = { "yyyy.MM.dd HH:mm:ss" }; // const string mt5Format = "yyyy.MM.dd HH:mm:ss"; // 2023.05.08 19:52:22 <- from MT5 
    
    private const string ok = "ok";
    private const int _batchSize = 1000;
    private const int minutes = 10;
    
    private readonly Administrator.Administrator _administrator;
   
    private readonly MediatorToTerminalClient _mediatorToTerminalClient;
    private readonly ReaderWriterLockSlim _queueLock = new();
    //private readonly IQuotationsRepository _quotationsRepository;
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();
    private readonly Timer _saveTimer;
    private readonly Quotation[] _lastKnownQuotations;

    public QuotationsProcessor(Administrator.Administrator administrator, MediatorToTerminalClient mediatorToTerminalClient
        //,IQuotationsRepository quotationsRepository
        )
    {
        _administrator = administrator;
        _mediatorToTerminalClient = mediatorToTerminalClient;
        //_quotationsRepository = quotationsRepository;

        _lastKnownQuotations = new Quotation[_administrator.TotalIndicators];

        _saveTimer = new Timer(minutes * 60 * 1000);
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        _saveTimer.AutoReset = true;
        _saveTimer.Enabled = true;
    }

    public void DeInit(Symbol symbol, DeInitReason reason)
    {
        _lastKnownQuotations[(int)symbol - 1] = Quotation.Empty;
        _administrator.Environments[(int)symbol - 1] = null;
        _administrator.ConnectedIndicators[(int)symbol - 1] = false;
        _administrator.DeInitReasons[(int)symbol - 1] = reason;
        Console.WriteLine($"Indicator {symbol} disconnected!");
    }

    public string Init(int symbol, string datetime, double ask, double bid, int environment)
    {
        if (_lastKnownQuotations[symbol - 1] == Quotation.Empty)
        {
            Debug.Assert(_administrator.Environments[symbol - 1] == null);
            _administrator.Environments[symbol - 1] = (Environment)environment;

            Debug.Assert(_administrator.ConnectedIndicators[symbol - 1] == false);
            _administrator.ConnectedIndicators[symbol - 1] = true;

            var resultSymbol = (Symbol)symbol;
            var resultDateTime = DateTime.ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
            var resultBid = Normalize(resultSymbol, Side.Bid, bid);
            var quotation = new Quotation(resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);
            _lastKnownQuotations[symbol - 1] = quotation;
        }
        else throw new Exception(Administrator.Administrator.MultipleConnections);

        if (_administrator.IndicatorsConnected) Console.WriteLine($"Indicators connected! Environment is {_administrator.Environment}");
        return ok;
    }

    public string Tick(int symbol, string datetime, double ask, double bid)
    {
        var resultSymbol = (Symbol)symbol;
        var resultDateTime = DateTime.ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        var resultAsk = Normalize(resultSymbol, Side.Ask, ask);
        var resultBid = Normalize(resultSymbol, Side.Bid, bid);
        var quotation = new Quotation(resultSymbol, resultDateTime, ask, bid, resultAsk, resultBid);

        //+milliseconds!!!!


        if (_lastKnownQuotations[symbol - 1].IntAsk == quotation.IntAsk && _lastKnownQuotations[symbol - 1].IntBid == quotation.IntBid)
        {
            return ok;
        }
        else
        {
            Console.WriteLine(quotation);
            _lastKnownQuotations[symbol - 1] = quotation;
        }



        //if (Normalize( symbol,  datetime, broker, ask, bid, out var newQuotation))
        // _mediatorToTerminalClient.Modification(newQuotation);

        //bool shouldSave;
        //_queueLock.EnterWriteLock();
        //try
        //{
        //    _latestQuotations.AddOrUpdate(symbol, quotation, (_, _) => quotation);
        //    _quotationsToSave.Enqueue(quotation);
        //    shouldSave = _quotationsToSave.Count >= _batchSize;
        //}
        //finally
        //{
        //    _queueLock.ExitWriteLock();
        //}

        //if (shouldSave) await SaveQuotationsAsync();

        //Console.WriteLine($"{symbol}|{datetime}|{ask}|{bid}");
        //Console.Write(".");
        return ok;
    }
   
    private async void OnSaveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await SaveQuotationsAsync();
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

        await SaveQuotationsToDatabase(quotationsToSave);
    }

    private async Task SaveQuotationsToDatabase(List<Quotation> quotationsToSave)
    {
        throw new NotImplementedException();

        //if (quotationsToSave.Count == 0) return;
        //var result = await _quotationsRepository.SaveQuotations(quotationsToSave);
        //Console.Write($"{result}|");
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
            case Side.Ask: if (Remainder >= 4) Quotient += 1; break;
            case Side.Bid: if (Remainder >= 6) Quotient += 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(side));
        }
        return Quotient;
    }
}


//private static DateTime ParseStringToDateTime(string datetime)
//{
//    DateTime resultDateTime;
//    if (DateTime.TryParse(datetime, out var parsedDateTime)) resultDateTime = parsedDateTime;
//    else throw new ArgumentException("The provided date string could not be parsed into a valid DateTime.");
//    if (resultDateTime.Kind != DateTimeKind.Utc) resultDateTime = resultDateTime.ToUniversalTime();
//    return resultDateTime;
//}




//        datetime = quotation.DateTime;
//        while (lastKnownQuotation.DateTime >= datetime) datetime = datetime.AddMilliseconds(1);