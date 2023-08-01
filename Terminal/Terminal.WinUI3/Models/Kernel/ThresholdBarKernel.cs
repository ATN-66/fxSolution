/*+------------------------------------------------------------------+
  |                                           Terminal.WinUI3.AI.Data|
  |                                            ThresholdBarKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Entities;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.Models.Kernel;

public class ThresholdBarKernel : Kernel<ThresholdBar>
{
    private const double StartingThreshold = 50d; // 5 pips 
    private readonly double _threshold;
    private readonly double _digit;

    public ThresholdBarKernel(int thresholdInPips, double digit)
    {
        _digit = digit;
        _threshold = thresholdInPips / _digit;
    }

    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        var quotationList = quotations.ToList();
        var bufferOpen = quotationList[0].Ask;
        var bufferDateTime = quotationList[0].DateTime;
        var started = false;

        foreach (var quotation in quotationList)
        {
            if (!started)
            {
                var difference = Math.Abs(quotation.Ask - bufferOpen) * _digit;
                if (difference < StartingThreshold)
                {
                    continue;
                }

                Items.Add(new ThresholdBar(bufferOpen, quotation.Ask)
                {
                    Symbol = quotation.Symbol,
                    DateTime = bufferDateTime,
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

    public override void Add(Quotation quotation)
    {
        var lastThresholdBar = Items[^1];

        switch (lastThresholdBar.Direction)
        {
            case Direction.Up:
                if (quotation.Ask > lastThresholdBar.Close)
                {
                    lastThresholdBar.Close = quotation.Ask;
                    lastThresholdBar.Threshold = quotation.Ask - _threshold;
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                else if (quotation.Ask < lastThresholdBar.Threshold)
                {
                    Items.Add(new ThresholdBar(lastThresholdBar.Close, quotation.Ask)
                    {
                        Symbol = quotation.Symbol,
                        DateTime = quotation.DateTime,
                        Threshold = quotation.Ask + _threshold,
                        Ask = quotation.Ask,
                        Bid = quotation.Bid
                    });
                }
                else
                {
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                break;
            case Direction.Down:
                if (quotation.Ask < lastThresholdBar.Close)
                {
                    lastThresholdBar.Close = quotation.Ask;
                    lastThresholdBar.Threshold = quotation.Ask + _threshold;
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                else if (quotation.Ask > lastThresholdBar.Threshold)
                {
                    Items.Add(new ThresholdBar(lastThresholdBar.Close, quotation.Ask)
                    {
                        Symbol = quotation.Symbol,
                        DateTime = quotation.DateTime,
                        Threshold = quotation.Ask - _threshold,
                        Ask = quotation.Ask,
                        Bid = quotation.Bid
                    });
                }
                else
                {
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                break;
            case Direction.NaN:
            default: throw new ArgumentOutOfRangeException($"{nameof(lastThresholdBar.Direction)}", @"Encountered a ThresholdBar with invalid direction.");
        }
    }
}
