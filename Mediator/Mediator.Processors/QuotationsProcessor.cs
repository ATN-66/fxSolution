/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                           QuotationsProcessor.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Timers;
using Common.Entities;
using Mediator.Client.Mediator.To.Terminal;
using Mediator.Repository.Interfaces;
using Microsoft.SqlServer.Server;
using Protos.Grpc;
using Environment = Common.Entities.Environment;
using Timer = System.Timers.Timer;

namespace Mediator.Processors;

public class QuotationsProcessor
{
    readonly string[] _formats = { "yyyy.MM.dd HH:mm:ss" }; // const string mt5Format = "yyyy.MM.dd HH:mm:ss"; // 2023.05.08 19:52:22 <- from MT5 
    private const string MultipleConnections = "Indicator cannot connect more that one time.";
    private const string ok = "ok";
    private const int _batchSize = 1000;
    private const int minutes = 10;
    private static readonly int _totalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    private readonly Administrator.Administrator _administrator;
    private readonly bool[] _connectedIndicators = new bool [_totalIndicators];
    private readonly DeInitReason?[] _deInitReasons = new DeInitReason?[_totalIndicators];
    private readonly Environment?[] _environments = new Environment?[_totalIndicators];
    private readonly ConcurrentDictionary<Symbol, Quotation> _latestQuotations = new();
    private readonly MediatorToTerminalClient _mediatorToTerminalClient;
    private readonly ReaderWriterLockSlim _queueLock = new();
    private readonly IQuotationsRepository _quotationsRepository;
    private readonly ConcurrentQueue<Quotation> _quotationsToSave = new();
    private readonly Timer _saveTimer;
    private readonly Queue<string> messages = new();
    private bool _allIndicatorsConnected;
    private readonly Quotation?[] _lastKnownQuotations = new Quotation?[_totalIndicators];

    public QuotationsProcessor(Administrator.Administrator administrator,
        MediatorToTerminalClient mediatorToTerminalClient,
        IQuotationsRepository quotationsRepository)
    {
        _administrator = administrator;
        _mediatorToTerminalClient = mediatorToTerminalClient;
        _quotationsRepository = quotationsRepository;

        _saveTimer = new Timer(minutes * 60 * 1000);
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        _saveTimer.AutoReset = true;
        _saveTimer.Enabled = true;
    }

    public void DeInit(Symbol symbol, DeInitReason reason)
    {
        _allIndicatorsConnected = false;
        _administrator.IndicatorsIsON = false;
        _connectedIndicators[(int)symbol - 1] = false;
        _environments[(int)symbol - 1] = null;
        _deInitReasons[(int)symbol - 1] = reason;
    }

    public string Init(int symbol, string datetime, double ask, double bid, int environment)
    {
        // 2022-05-01 21:05:45:475 <- in .csv (tickstory-ducascopy)
        // 2022-01-10 00:00:00.2530000 <- in db testing.unmodified (tickstory-ducascopy)
        // 2023.05.08 19:52:22 <- from MT5
        // 2023.02.26 22:00:00 <- from simulator (transformed)

        if (_lastKnownQuotations[symbol - 1] == null)
        {
            Debug.Assert(_environments[symbol - 1] == null);
            _environments[symbol - 1] = (Environment)environment;

            var resultSymbol = (Symbol)symbol;
            var resultDateTime = DateTime.ParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            double resultAsk = ModifyNumber(ask);
            double resultBid;

            switch (resultSymbol)
            {
                case Symbol.EURGBP:
                case Symbol.EURUSD:
                case Symbol.GBPUSD:
                    resultAsk = (int)(ask * 100_000);
                    resultBid = (int)(bid * 100_000);
                    break;
                case Symbol.EURJPY:
                case Symbol.GBPJPY:
                case Symbol.USDJPY:
                    resultAsk = (int)(ask * 1_000);
                    resultBid = (int)(bid * 1_000);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            //wrong! if 0 last digit?
            throw new NotImplementedException();

            resultAsk = (resultAsk / 10 + 1) * 10; //Act as ceiling
            resultBid = resultBid / 10 * 10; //Act as a flooring
        }
        else throw new Exception(MultipleConnections);




        //_connectedIndicators[(int)symbol - 1] = true;
       
       

        //_allIndicatorsConnected = true;
        //for (var i = 1; i <= _totalIndicators; i++)
        //{
        //    if (_connectedIndicators[i - 1]) continue;
        //    _allIndicatorsConnected = false;
        //    break;
        //}

        //if (!_allIndicatorsConnected) return await Task.FromResult(ok);
        //Environment = _environments[0];
        //for (var i = 1; i < _totalIndicators; i++)
        //{
        //    if (_environments[i] == Environment) continue;
        //    throw new Exception("Client's environments are not the same.");
        //}

        //switch (Environment)
        //{
        //    case Common.Entities.Environment.Development:
        //        Console.WriteLine(DevelopmentConnectedMessage);
        //        break;
        //    case Common.Entities.Environment.Production:
        //        Console.WriteLine(ProductionConnectedMessage);
        //        break;
        //    default: throw new NotImplementedException($"Environment:{Environment} is not in use.");
        //}

        //IndicatorsIsON = _allIndicatorsConnected;
        return ok;



    }

    public static double ModifyNumber(double input)
    {
        int lastDigit = (int)(input * 100000) % 10;
        if (lastDigit != 0)
        {
            input = Math.Floor(input * 10000) / 10000 + 0.0001;
        }
        return input;
    }




    public string Tick(int symbol, string datetime, double ask, double bid)
    {
        //if (Normalize( symbol,  datetime, broker, ask, bid, out var newQuotation))
        // _mediatorToTerminalClient.Tick(newQuotation);

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

        Console.WriteLine($"{symbol}|{datetime}|{ask:000.00000}|{bid:000.00000}");
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
        if (quotationsToSave.Count == 0) return;
        var result = await _quotationsRepository.SaveQuotations(quotationsToSave);
        Console.Write($"{result}|");
    }

    private bool Normalize(Quotation quotation, out Quotation normalizedQuotation)
    {
        var symbolIndex = (int)quotation.Symbol;
        var lastKnownQuotation = _lastKnownQuotations[symbolIndex - 1]!.Value;

        var askDiff = Math.Abs(quotation.Ask - lastKnownQuotation.Ask);
        var bidDiff = Math.Abs(quotation.Bid - lastKnownQuotation.Bid);

        if (askDiff > 5 || bidDiff > 5)
        {
            var newAsk = quotation.Ask - (quotation.Ask % 10);
            var newBid = quotation.Bid - (quotation.Bid % 10);

            if (quotation.Ask > lastKnownQuotation.Ask)
            {
                while (askDiff > 5)
                {
                    newAsk += 10;
                    askDiff -= 10;
                }
            }
            else
            {
                while (askDiff > 5)
                {
                    newAsk -= 10;
                    askDiff -= 10;
                }
            }

            if (quotation.Bid > lastKnownQuotation.Bid)
            {
                while (bidDiff > 5)
                {
                    newBid += 10;
                    bidDiff -= 10;
                }
            }
            else
            {
                while (bidDiff > 5)
                {
                    newBid -= 10;
                    bidDiff -= 10;
                }
            }

            normalizedQuotation = new Quotation(quotation.Symbol, quotation.DateTime, newAsk, newBid);
            _lastKnownQuotations[symbolIndex - 1] = normalizedQuotation;
            return true;

            //if (_latestQuotations.TryGetValue(symbol, out var q))
            //    while (q.DateTime >= datetime)
            //        datetime = datetime.AddMilliseconds(1);
            //var quotation = new Quotation(symbol, datetime, broker, ask, bid);//todo: into normalize


        }

        normalizedQuotation = default;
        return false;
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