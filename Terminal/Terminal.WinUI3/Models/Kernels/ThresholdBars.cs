
/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Drawing;
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
    private const int Ratio = 3; // todo

    public ThresholdBars(Symbol symbol, int thresholdInPips, int digit, IImpulsesKernel impulses, IFileService fileService) : base(fileService)
    {
        _symbol = symbol;
        _digit = digit;
        _threshold = thresholdInPips / (double)_digit;
        _impulses = impulses;
        _startingBarLength = thresholdInPips * Ratio; 
    }

    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        var quotationList = quotations.ToList();
        var open = quotationList[0].Price(Direction.Down, _digit);
        var start = quotationList[0].Start;
        var done = false;

        foreach (var quotation in quotationList)
        {
            if (!done)
            {
                var close = quotation.Price(Direction.Down, _digit);
                var difference = Math.Abs(close - open) * _digit;
                if (difference < _startingBarLength)
                {
                    continue;
                }

                var direction = close > open ? Direction.Up : Direction.Down;
                ThresholdBar tbar;
                switch (direction)
                {
                    case Direction.Up:

                        tbar = new ThresholdBar(Force.Initiation | Force.Nothing | Force.Up, ++_tBarID, 0, _symbol, open, open + _threshold, start, start) { Ask = open + _threshold, Bid = open + _threshold };
                        //
                        tbar.Force = Force.Extension | Force.Nothing | Force.Up;
                        tbar.Close = close;
                        tbar.End = quotation.End;
                        tbar.Threshold = close - _threshold;
                        break;
                    case Direction.Down:
                        tbar = new ThresholdBar(Force.Initiation | Force.Nothing | Force.Down, ++_tBarID, 0, _symbol, open, open - _threshold, start, start) { Ask = open - _threshold, Bid = open - _threshold };
                        //
                        tbar.Force = Force.Nothing | Force.Extension | Force.Down;
                        tbar.Close = close;
                        tbar.End = quotation.End;
                        tbar.Threshold = close + _threshold;
                        break;
                    case Direction.NaN:
                    default: throw new ArgumentOutOfRangeException();
                }

                tbar.Ask = quotation.Ask;
                tbar.Bid = quotation.Bid;

                Items.Add(tbar);
                done = true;
                continue;
            }

            Add(quotation);
        }
    }
    public override void Add(Quotation quotation)
    {
        var present = Items[^1];
        var price = quotation.Price(present.Direction, _digit);
        ThresholdBar tbar;

        if (Count <= 1)
        {
            switch (present.Direction)
            {
                case Direction.Up:
                    if (price > present.Close)
                    {
                        present.Close = price;
                        present.End = quotation.End;
                        present.Threshold = price - _threshold;
                        present.Ask = quotation.Ask;
                        present.Bid = quotation.Bid;
                    }
                    else if (price <= present.Threshold)
                    {
                        tbar = new ThresholdBar(Force.Retracement | Force.Initiation | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                        Items.Add(tbar);
                        _impulses.OnInitialization(Items[^2], Items[^1]);
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
                        present.Threshold = price + _threshold;
                        present.Ask = quotation.Ask;
                        present.Bid = quotation.Bid;
                    }
                    else if (price >= present.Threshold)
                    {
                        tbar = new ThresholdBar(Force.Initiation | Force.Retracement | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close,price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                        Items.Add(tbar);
                        _impulses.OnInitialization(Items[^2], Items[^1]);
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
        }
        else
        {
            var previous = Items[^2];
            switch (present.Direction)
            {
                case Direction.Up:
                    if (price > present.Close)
                    {
                        switch (present.Force)
                        {
                            case Force.Initiation | Force.Retracement | Force.Down:
                                if (price > previous.Open)
                                {
                                    present.Close = previous.Open;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Initiation));
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Initiation));
                                }
                                break;
                            case Force.OppositeRecovery | Force.NegativeSideWay | Force.Down:
                                if (price > previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close < previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.OppositePositiveSideWay | Force.NegativeSideWay | Force.Down:
                                if (price > previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close < previous.Open);
                                        throw new NotImplementedException("!present.Close.Equals(previous.Open)");
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.Recovery | Force.OppositeRetracement | Force.Up:
                                if (price > previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close < previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.PositiveSideWay | Force.OppositeNegativeSideWay | Force.Up:
                                if (price > previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close < previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Up;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price - _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.Extension | Force.Nothing | Force.Up:
                                present.Close = price;
                                present.End = quotation.End;
                                present.Threshold = price - _threshold;
                                _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (price <= present.Threshold)
                    {
                        switch (present.Force)
                        {
                            case Force.Initiation | Force.Retracement | Force.Down:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeRetracement | Force.Recovery | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.OppositeRetracement | Force.Recovery | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.OppositeRecovery | Force.NegativeSideWay | Force.Down:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.OppositePositiveSideWay | Force.NegativeSideWay | Force.Down:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.Recovery | Force.OppositeRetracement | Force.Up:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositeRecovery | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositeRecovery | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.PositiveSideWay | Force.OppositeNegativeSideWay | Force.Up:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositePositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositePositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.Extension | Force.Nothing | Force.Up:
                                if (price < present.Open)
                                {
                                    tbar = new ThresholdBar(Force.Retracement | Force.Initiation | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.Retracement | Force.Initiation | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case Direction.Down:
                    if (price < present.Close)
                    {
                        switch (present.Force)
                        {
                            case Force.Initiation | Force.Retracement | Force.Up:
                                if (price < previous.Open)
                                {
                                    present.Close = previous.Open;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Initiation));
                                    present.Force = Force.Extension | Force.Nothing | Force.Down;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Initiation));
                                }
                                break;
                            case Force.OppositeRecovery | Force.NegativeSideWay | Force.Up:
                                if (price < previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close > previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.OppositePositiveSideWay | Force.NegativeSideWay | Force.Up:
                                if (price < previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close > previous.Open);
                                        throw new NotImplementedException("!present.Close.Equals(previous.Open)");
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Change));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.Recovery | Force.OppositeRetracement | Force.Down:
                                if (price < previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close > previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.PositiveSideWay | Force.OppositeNegativeSideWay | Force.Down:
                                if (price < previous.Open)
                                {
                                    if (!present.Close.Equals(previous.Open))
                                    {
                                        Debug.Assert(present.Close > previous.Open);
                                        present.Close = previous.Open;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                    else
                                    {
                                        present.Force = Force.Extension | Force.Nothing | Force.Down;
                                        present.Close = price;
                                        present.End = quotation.End;
                                        present.Threshold = price + _threshold;
                                        _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    }
                                }
                                else
                                {
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price + _threshold;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                }
                                break;
                            case Force.Extension | Force.Nothing | Force.Down:
                                present.Close = price;
                                present.End = quotation.End;
                                present.Threshold = price + _threshold;
                                _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (price >= present.Threshold)
                    {
                        switch (present.Force)
                        {
                            case Force.Initiation | Force.Retracement | Force.Up:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeRetracement | Force.Recovery | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.Recovery | Force.OppositeRetracement | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.OppositeRecovery | Force.NegativeSideWay | Force.Up:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.OppositePositiveSideWay | Force.NegativeSideWay | Force.Up:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.OppositeNegativeSideWay | Force.PositiveSideWay | Force.Up, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.Recovery | Force.OppositeRetracement | Force.Down:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositeRecovery | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositeRecovery | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.PositiveSideWay | Force.OppositeNegativeSideWay | Force.Down:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositePositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.NegativeSideWay | Force.OppositePositiveSideWay | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    return;
                                }
                            case Force.Extension | Force.Nothing | Force.Down:
                                if (price > present.Open)
                                {
                                    tbar = new ThresholdBar(Force.Retracement | Force.Initiation | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, present.Open, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
                                    present = Items[^1];
                                    present.Force = Force.Extension | Force.Nothing | Force.Up;
                                    present.Close = price;
                                    present.End = quotation.End;
                                    present.Threshold = price - _threshold;
                                    present.Ask = quotation.Ask;
                                    present.Bid = quotation.Bid;
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Extreme));
                                    return;
                                }
                                else
                                {
                                    tbar = new ThresholdBar(Force.Retracement | Force.Initiation | Force.Down, ++_tBarID, _threshold, quotation.Symbol, present.Close, price, present.End, quotation.End) { Ask = quotation.Ask, Bid = quotation.Bid };
                                    Items.Add(tbar);
                                    _impulses.Add(new Transition(++_transID, present, TransitionType.Fluctuation));
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
    }
   
    public override int FindIndex(DateTime dateTime)
    {
        throw new NotImplementedException("ThresholdBars:FindIndex");
    }
    public override ThresholdBar? FindItem(DateTime dateTime)
    {
        return Items.FirstOrDefault(t => dateTime >= t.Start && dateTime <= t.End);
    }
    public override void SaveItems((DateTime first, DateTime second) dateRange)
    {
        var items = Items.Where(t => t.Start >= dateRange.first && t.End <= dateRange.second);
        SaveItemsToJson(items, _symbol, GetType().Name.ToLower());
    }
}