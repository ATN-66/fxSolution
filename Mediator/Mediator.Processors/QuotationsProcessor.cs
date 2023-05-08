/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                           QuotationsProcessor.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using Common.Entities;
using Mediator.Client.Mediator.To.Terminal;
using Mediator.Repository.Interfaces;
using Protos.Grpc;
using Environment = Common.Entities.Environment;
using Timer = System.Timers.Timer;

namespace Mediator.Processors;

public class QuotationsProcessor
{
    private const string ok = "ok";
    private const string DevelopmentMachineName = "UNIT1065";
    private const string ProductionMachineName = "UNIT1068";
    private const string DevelopmentConnectedMessage = " MT5(Development) connected. ";
    private const string ProductionConnectedMessage = " MT5(Production) connected. ";

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

    private Environment? Environment
    {
        get => _administrator.Environment;
        set => _administrator.Environment = value;
    }

    private bool IndicatorsIsON
    {
        set => _administrator.IndicatorsIsON = value;
    }

    public void DeInit(Symbol symbol, DeInitReason reason)
    {
        Environment = null;
        _allIndicatorsConnected = false;
        IndicatorsIsON = false;
        _connectedIndicators[(int)symbol - 1] = false;
        _environments[(int)symbol - 1] = null;
        _deInitReasons[(int)symbol - 1] = reason;
    }

    public string Init(Quotation quotation, Environment environment)
    {
        Console.Write(".");
        //if (_lastKnownQuotations[(int)symbol - 1] == null)
        //{
        //    _lastKnownQuotations[(int)symbol - 1] =
        //        new Quotation(symbol, datetime, broker, (ask / 10) * 10, (bid / 10) * 10);//last digit to zero
        //}
        //else throw new Exception("Indicator cannot connect more that one time.");

        //_connectedIndicators[(int)symbol - 1] = true;
        //_environments[(int)symbol - 1] = machineName switch
        //{
        //    DevelopmentMachineName => Common.Entities.Environment.Development,
        //    ProductionMachineName => Common.Entities.Environment.Production,
        //    _ => throw new Exception("Machine Name is unknown.")
        //};

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

    public string Tick(Quotation quotation)
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

        Console.WriteLine(quotation);
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