
/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Entities;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Models.Kernels;

public class ThresholdBars : DataSourceKernel<ThresholdBar>
{
    private readonly Symbol _symbol;
    private const double StartingThreshold = 50d; // 5 pips //todo: settings
    private readonly double _threshold;
    private readonly double _digit;

    public ThresholdBars(Symbol symbol, int thresholdInPips, double digit, IFileService fileService) : base(fileService)
    {
        _symbol = symbol;
        _digit = digit;
        _threshold = thresholdInPips / _digit;
    }

    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        try
        {
            var quotationList = quotations.ToList();
            var bufferOpen = quotationList[0].Ask;
            bufferOpen = RoundNumber(bufferOpen, Direction.Down);
            var bufferDateTime = quotationList[0].Start;
            var started = false;

            foreach (var quotation in quotationList)
            {
                if (!started)
                {
                    var ask = RoundNumber(quotation.Ask, Direction.Down);
                    var difference = Math.Abs(ask - bufferOpen) * _digit;
                    if (difference < StartingThreshold)
                    {
                        continue;
                    }

                    var direction = ask > bufferOpen ? Direction.Up : Direction.Down;
                    var force = new DualForce
                    {
                        UpForce = direction == Direction.Up ? Force.Extension : Force.Nothing,
                        DownForce = direction == Direction.Down ? Force.Extension : Force.Nothing
                    };

                    Items.Add(new ThresholdBar(bufferOpen, ask)
                    {
                        Force = force,
                        Symbol = quotation.Symbol,
                        Start = bufferDateTime,
                        End = quotation.End,
                        Ask = quotation.Ask,
                        Bid = quotation.Bid
                    });

                    if (Items[^1].Direction == Direction.Up)
                    {
                        Items[^1].Threshold = Items[^1].Close - _threshold;
                    }
                    else
                    {
                        Items[^1].Threshold = Items[^1].Close + _threshold;
                    }

                    started = true;
                    continue;
                }

                Add(quotation);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
    }

    public override void Add(Quotation quotation)
    {
        double ask;
        if (Count <= 1)
        {
            var present = Items[^1];
            ask = RoundNumber(quotation.Ask, present.Direction);
            switch (present.Direction)
            {
                case Direction.Up:
                    if (ask > present.Close)
                    {
                        present.End = quotation.End;
                        present.Close = ask;
                        present.Threshold = ask - _threshold;
                        present.Ask = quotation.Ask;
                        present.Bid = quotation.Bid;
                    }
                    else if (ask <= present.Threshold)
                    {
                        present.End = quotation.Start;
                        var force = new DualForce
                        {
                            UpForce = Force.Retracement,
                            DownForce = Force.Initiation
                        };
                        Items.Add(new ThresholdBar(present.Close, ask)
                        {
                            Force = force,
                            Symbol = quotation.Symbol,
                            Start = quotation.Start,
                            End = quotation.End,
                            Threshold = ask + _threshold,
                            Ask = quotation.Ask,
                            Bid = quotation.Bid
                        });
                    }
                    else
                    {
                        present.End = quotation.End;
                        present.Ask = quotation.Ask;
                        present.Bid = quotation.Bid;
                    }
                    break;
                case Direction.Down:
                    if (ask < present.Close)
                    {
                        present.End = quotation.End;
                        present.Close = ask;
                        present.Threshold = ask + _threshold;
                        present.Ask = quotation.Ask;
                        present.Bid = quotation.Bid;
                    }
                    else if (ask >= present.Threshold)
                    {
                        present.End = quotation.Start;
                        var force = new DualForce
                        {
                            UpForce = Force.Initiation,
                            DownForce = Force.Retracement
                        };
                        Items.Add(new ThresholdBar(present.Close, ask)
                        {
                            Force = force,
                            Symbol = quotation.Symbol,
                            Start = quotation.Start,
                            End = quotation.End,
                            Threshold = ask - _threshold,
                            Ask = quotation.Ask,
                            Bid = quotation.Bid
                        });
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
            var present = Items[^1];
            var previous = Items[^2];
            ask = RoundNumber(quotation.Ask, present.Direction);
            DualForce force;
            switch (present.Direction)
            {
                case Direction.Up:
                    if (ask > present.Close)
                    {
                        switch (present.Force.UpForce)
                        {
                            case Force.Initiation:
                                Debug.Assert(present.Force.DownForce == Force.Retracement);
                                if (ask > previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing };
                                }
                                present.Close = ask;
                                present.Threshold = ask - _threshold;
                                break;
                            case Force.Recovery:
                                Debug.Assert(present.Force.DownForce is Force.Retracement or Force.NegativeSideWay);
                                if (ask > previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing };
                                }
                                present.Close = ask;
                                present.Threshold = ask - _threshold;
                                break;
                            case Force.Extension:
                                present.Close = ask;
                                present.Threshold = ask - _threshold;
                                break;
                            case Force.PositiveSideWay:
                                Debug.Assert(present.Force.DownForce is Force.NegativeSideWay);
                                if (ask > previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing };
                                }
                                present.Close = ask;
                                present.Threshold = ask - _threshold;
                                break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (ask <= present.Threshold)
                    {
                        switch (present.Force.UpForce)
                        {
                            case Force.Initiation:
                                Debug.Assert(present.Force.DownForce == Force.Retracement);
                                present.End = quotation.Start;
                                force = ask < present.Open ? new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension } : new DualForce { UpForce = Force.Retracement, DownForce = Force.Recovery };
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask + _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.Recovery:
                                present.End = quotation.Start;
                                if (ask < present.Open)
                                {
                                    force = new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension };
                                }
                                else
                                {
                                    force = present.Force.DownForce switch
                                    {
                                        Force.Retracement => new DualForce { UpForce = Force.NegativeSideWay, DownForce = Force.Recovery },
                                        Force.NegativeSideWay => new DualForce { UpForce = Force.NegativeSideWay, DownForce = Force.PositiveSideWay },
                                        _ => throw new ArgumentOutOfRangeException()
                                    };
                                }
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask + _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.Extension:
                                present.End = quotation.Start;
                                force = ask < present.Open ? new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension } : new DualForce { UpForce = Force.Retracement, DownForce = Force.Initiation };
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask + _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.PositiveSideWay:
                                present.End = quotation.Start;
                                Debug.Assert(present.Force.DownForce is Force.NegativeSideWay);
                                if (ask < present.Open)
                                {
                                    force = new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension };
                                }
                                else
                                {
                                    force = new DualForce { UpForce = Force.NegativeSideWay, DownForce = Force.PositiveSideWay };
                                }
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask + _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case Direction.Down:
                    if (ask < present.Close)
                    {
                        switch (present.Force.DownForce)
                        {
                            case Force.Initiation:
                                Debug.Assert(present.Force.UpForce == Force.Retracement);
                                if (ask < previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension };
                                }
                                present.Close = ask;
                                present.Threshold = ask + _threshold;
                                break;
                            case Force.Recovery:
                                Debug.Assert(present.Force.UpForce is Force.Retracement or Force.NegativeSideWay);
                                if (ask < previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension };
                                }
                                present.Close = ask;
                                present.Threshold = ask + _threshold;
                                break;
                            case Force.Extension:
                                present.Close = ask;
                                present.Threshold = ask + _threshold;
                                break;
                            case Force.PositiveSideWay:
                                Debug.Assert(present.Force.UpForce is Force.NegativeSideWay);
                                if (ask < previous.Open)
                                {
                                    present.Force = new DualForce { UpForce = Force.Nothing, DownForce = Force.Extension };
                                }
                                present.Close = ask;
                                present.Threshold = ask + _threshold;
                                break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (ask >= present.Threshold)
                    {
                        switch (present.Force.DownForce)
                        {
                            case Force.Initiation:
                                Debug.Assert(present.Force.UpForce == Force.Retracement);
                                present.End = quotation.Start;
                                force = ask > present.Open ? new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing } : new DualForce { UpForce = Force.Recovery, DownForce = Force.Retracement };
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask - _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.Recovery:
                                present.End = quotation.Start;
                                if (ask > present.Open)
                                {
                                    force = new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing };
                                }
                                else
                                {
                                    force = present.Force.UpForce switch
                                    {
                                        Force.Retracement => new DualForce { UpForce = Force.Recovery, DownForce = Force.NegativeSideWay },
                                        Force.NegativeSideWay => new DualForce { UpForce = Force.PositiveSideWay, DownForce = Force.NegativeSideWay },
                                        _ => throw new ArgumentOutOfRangeException()
                                    };
                                }
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask - _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.Extension:
                                present.End = quotation.Start;
                                force = ask > present.Open ? new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing } : new DualForce { UpForce = Force.Initiation, DownForce = Force.Retracement };
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask - _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            case Force.PositiveSideWay:
                                present.End = quotation.Start;
                                Debug.Assert(present.Force.UpForce is Force.NegativeSideWay);
                                if (ask > present.Open)
                                {
                                    force = new DualForce { UpForce = Force.Extension, DownForce = Force.Nothing };
                                }
                                else
                                {
                                    force = new DualForce { UpForce = Force.PositiveSideWay, DownForce = Force.NegativeSideWay };
                                }
                                Items.Add(new ThresholdBar(present.Close, ask)
                                {
                                    Force = force,
                                    Symbol = quotation.Symbol,
                                    Start = quotation.Start,
                                    End = quotation.End,
                                    Threshold = ask - _threshold,
                                    Ask = quotation.Ask,
                                    Bid = quotation.Bid
                                });
                                return;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case Direction.NaN:
                default: throw new ArgumentOutOfRangeException($"{nameof(present.Direction)}", @"Encountered a ThresholdBars with invalid direction.");
            }
            present.End = quotation.End;
            present.Ask = quotation.Ask;
            present.Bid = quotation.Bid;
        }
    }

    private double RoundNumber(double number, Direction direction)
    {
        var tempNumber = number * _digit;
        var lastDigit = (int)tempNumber % 10;

        tempNumber = direction switch
        {
            Direction.Down when lastDigit <= 5 => Math.Floor(tempNumber / 10) * 10,
            Direction.Down => Math.Floor(tempNumber / 10) * 10 + 5,
            Direction.Up when lastDigit < 5 => Math.Floor(tempNumber / 10) * 10 - 5,
            Direction.Up => Math.Floor(tempNumber / 10) * 10,
            Direction.NaN => throw new ArgumentOutOfRangeException(nameof(direction), direction, @"invalid direction"),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, @"invalid direction")
        };

        return tempNumber / _digit;
    }
    public override int FindIndex(DateTime dateTime)
    {
        throw new NotImplementedException("ThresholdBars:FindIndex");
    }
    public override ThresholdBar? FindItem(DateTime dateTime)
    {
        return Items.FirstOrDefault(t => dateTime >= t.Start && dateTime <= t.End);
    }
    public override void SaveUnits((DateTime first, DateTime second) dateRange)
    {
        var items = Items.Where(t => t.Start >= dateRange.first && t.End <= dateRange.second);
        SaveItemsToJson(items, _symbol, GetType().Name.ToLower());
    }
}