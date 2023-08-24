
/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Diagnostics;
using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Entities;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Models.Kernels;

public class ThresholdBars : DataSourceKernel<ThresholdBar>
{
    private readonly Symbol _symbol;
    private readonly IImpulsesKernel _impulses;
    private readonly double _startingBarLength;
    private readonly double _threshold;
    private readonly int _digit;
    private int _tBarID;
    private int _transID;
    private const int Ratio = 5; // todo configurable
    private readonly BlockingCollection<Quotation> _quotations = new();

    private bool _stop;

    public ThresholdBars(Symbol symbol, int thresholdInPips, int digit, IImpulsesKernel impulses, IFileService fileService) : base(fileService)
    {
        _symbol = symbol;
        _digit = digit;
        _threshold = thresholdInPips / (double)_digit;
        _impulses = impulses;
        _startingBarLength = thresholdInPips * Ratio;
    }

    public void StartAsync(CancellationToken token)
    {
        Task.Run(async () =>
        {
            try
            {
                await foreach (var quotation in _quotations.GetConsumingAsyncEnumerable(token))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Process(quotation);
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw;
            }
        }, token);
    }

    public override void AddRange(List<Quotation> quotations)
    {
        var open = quotations[0].Price(Direction.Down, _digit);
        var start = quotations[0].Start;
        var done = false;

        foreach (var quotation in quotations)
        {
            if (!done)
            {
                var close = quotation.Price(Direction.Down, _digit);
                var difference = Math.Abs(close - open) * _digit;
                if (difference < _startingBarLength)
                {
                    continue;
                }

                Items.Add(new ThresholdBar(++_tBarID, _symbol, open, close, start, quotation.End, _threshold) { Ask = quotation.Ask, Bid = quotation.Bid });
                done = true;
                continue;
            }

            var present = Items[^1];
            var price = quotation.Price(present.Direction, _digit);

            if (Count <= 1)
            {
                ThresholdBar tBar;
                switch (present.Direction)
                {
                    case Direction.Up:
                        if (price > present.Close)
                        {
                            present.Close = price;
                            present.End = quotation.End;
                            present.Ask = quotation.Ask;
                            present.Bid = quotation.Bid;
                        }
                        else if (price <= present.Threshold)
                        {
                            tBar = new ThresholdBar(Stage.RetracementStart | Stage.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                            Items.Add(tBar);

                            var f = Items[^2];
                            var s = Items[^1];
                            Debug.Assert(f.Length > s.Length);

                            _impulses.OnInitialization(f, s);
                        }
                        else
                        {
                            present.End = quotation.End;
                            present.Ask = quotation.Ask;
                            present.Bid = quotation.Bid;
                        }
                        break;
                    case Direction.Down:
                        if (price < present.Close)
                        {
                            present.Close = price;
                            present.End = quotation.End;
                            present.Ask = quotation.Ask;
                            present.Bid = quotation.Bid;
                        }
                        else if (price >= present.Threshold)
                        {
                            tBar = new ThresholdBar(Stage.RetracementStart | Stage.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                            Items.Add(tBar);

                            var f = Items[^2];
                            var s = Items[^1];
                            Debug.Assert(f.Length > s.Length);

                            _impulses.OnInitialization(f, s);
                        }
                        else
                        {
                            present.End = quotation.End;
                            present.Ask = quotation.Ask;
                            present.Bid = quotation.Bid;
                        }
                        break;
                    case Direction.NaN:
                    default: throw new ArgumentOutOfRangeException($"{nameof(present.Direction)}", @"Encountered a ThresholdBars with invalid direction.");
                }

                continue;
            }

            Process(quotation);
        }
    }

    public override void Add(Quotation quotation)
    {
        _quotations.Add(quotation);
    }

    private void Process(Quotation quotation)
    {
        //if (_symbol != Symbol.EURGBP) { return; }//todo: remove
        if (_stop)
        {
            return;
        } //todo: remove

        ThresholdBar present;
        ThresholdBar previous;
        double price;
        ThresholdBar tBar;

        try
        {
            present = Items[^1];
            previous = Items[^2];
            price = quotation.Price(present.Direction, _digit);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }

        switch (present.Direction)
        {
            case Direction.Up:
                if (price > present.Close)
                {
                    switch (present.Stage)
                    {
                        case Stage.RetracementStart | Stage.Down:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.RetracementDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RetracementDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.RetracementContinue | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RetracementContinue | Stage.Down:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.RetracementDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RetracementDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RetracementDone | Stage.Down:
                            Debug.Assert(price > previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Up;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.NegativeSideWayStart | Stage.Down:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.NegativeSideWayContinue | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.NegativeSideWayContinue | Stage.Down:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.NegativeSideWayDone | Stage.Down:
                            Debug.Assert(price > previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Up;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.RecoveryStart | Stage.Up:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.RecoveryContinue | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.RecoveryContinue | Stage.Up:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.RecoveryDone | Stage.Up:
                            Debug.Assert(price > previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Up;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.PositiveSideWayStart | Stage.Up:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.PositiveSideWayContinue | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.PositiveSideWayContinue | Stage.Up:
                            if (price > previous.Open)
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            break;
                        case Stage.PositiveSideWayDone | Stage.Up:
                            Debug.Assert(price > previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Up;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.ExtensionStart | Stage.Up:
                            present.Stage = Stage.ExtensionContinue | Stage.Up;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.ExtensionContinue | Stage.Up:
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                else if (price <= present.Threshold)
                {
                    switch (present.Stage)
                    {
                        case Stage.RetracementStart | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RetracementContinue | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RetracementDone | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayStart | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayContinue | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayDone | Stage.Down:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryStart | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryContinue | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryDone | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayStart | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayContinue | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayDone | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.ExtensionStart | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RetracementStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.ExtensionContinue | Stage.Up:
                            if (price < present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RetracementStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                break;
            case Direction.Down:
                if (price < present.Close)
                {
                    switch (present.Stage)
                    {
                        case Stage.RetracementStart | Stage.Up:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.RetracementDone | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present)); //todo: check
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RetracementDone | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.RetracementContinue | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RetracementContinue | Stage.Up:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.RetracementDone | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present)); //todo: check
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RetracementDone | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RetracementDone | Stage.Up:
                            Debug.Assert(price < previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Down;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.NegativeSideWayStart | Stage.Up:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.NegativeSideWayContinue | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.NegativeSideWayContinue | Stage.Up:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.NegativeSideWayDone | Stage.Up;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.NegativeSideWayDone | Stage.Up:
                            Debug.Assert(price < previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Down;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.RecoveryStart | Stage.Down:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.RecoveryContinue | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RecoveryContinue | Stage.Down:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.RecoveryDone | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.RecoveryDone | Stage.Down:
                            Debug.Assert(price < previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Down;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.PositiveSideWayStart | Stage.Down:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Stage = Stage.PositiveSideWayContinue | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.PositiveSideWayContinue | Stage.Down:
                            if (price < previous.Open)
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Down;
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else if (price.Equals(previous.Open))
                            {
                                present.Stage = Stage.PositiveSideWayDone | Stage.Down;
                                present.Close = previous.Open;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }
                            else
                            {
                                present.Close = price;
                                present.End = quotation.End;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                            }

                            break;
                        case Stage.PositiveSideWayDone | Stage.Down:
                            Debug.Assert(price < previous.Open);
                            present.Stage = Stage.ExtensionStart | Stage.Down;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.ExtensionStart | Stage.Down:
                            present.Stage = Stage.ExtensionContinue | Stage.Down;
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        case Stage.ExtensionContinue | Stage.Down:
                            present.Close = price;
                            present.End = quotation.End;
                            _stop = _impulses.Add(new Transition(++_transID, present));
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                else if (price >= present.Threshold)
                {
                    switch (present.Stage)
                    {
                        case Stage.RetracementStart | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RetracementContinue | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RetracementDone | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RecoveryDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RecoveryStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayStart | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayContinue | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.NegativeSideWayDone | Stage.Up:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayDone | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.PositiveSideWayStart | Stage.Up, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryStart | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryContinue | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.RecoveryDone | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayStart | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayContinue | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.PositiveSideWayDone | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.NegativeSideWayStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.ExtensionStart | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RetracementStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        case Stage.ExtensionContinue | Stage.Down:
                            if (price > present.Open)
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                present.Stage = Stage.ExtensionStart | Stage.Up;
                                present.Close = price;
                                present.End = quotation.End;
                                present.Ask = quotation.Ask;
                                present.Bid = quotation.Bid;
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else if (price.Equals(present.Open))
                            {
                                tBar = new ThresholdBar(Stage.RetracementDone | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, present.Open, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                            else
                            {
                                tBar = new ThresholdBar(Stage.RetracementStart | Stage.Down, ++_tBarID, _threshold,
                                        quotation.Symbol, present.Close, price, present.End, quotation.End)
                                    { Ask = quotation.Ask, Bid = quotation.Bid };
                                Items.Add(tBar);
                                present = Items[^1];
                                _stop = _impulses.Add(new Transition(++_transID, present));
                                return;
                            }
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                break;
            case Direction.NaN:
            default: throw new ArgumentOutOfRangeException($"{nameof(present.Direction)}", @"Encountered a ThresholdBars with invalid direction.");
        }

        present.Ask = quotation.Ask;
        present.Bid = quotation.Bid;
    }

    public override int FindIndex(DateTime dateTime)
    {
        throw new NotImplementedException("ThresholdBars:FindIndex");
    }
    public override ThresholdBar? FindItem(DateTime dateTime)
    {
        return Items.FirstOrDefault(t => dateTime >= t.Start && dateTime <= t.End);
    }
    public override void Save((DateTime first, DateTime second) dateRange)
    {
        var items = Items.Where(t => t.Start >= dateRange.first && t.End <= dateRange.second);
        SaveItemsToJson(items, _symbol, GetType().Name.ToLower());
    }
}