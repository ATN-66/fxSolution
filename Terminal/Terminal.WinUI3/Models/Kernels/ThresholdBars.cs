/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                 ThresholdBars.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Entities;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.Models.Kernels;

public class ThresholdBars : DataSourceKernel<ThresholdBar>
{
    //private readonly IFileService _fileService;
    //private readonly Symbol _symbol;
    private const double StartingThreshold = 50d; // 5 pips //todo: settings
    private readonly double _threshold;
    private readonly double _digit;

    public ThresholdBars(int thresholdInPips, double digit)
    {
        //_fileService = fileService;IFileService fileService//Symbol symbol,
        //_symbol = symbol;
        _digit = digit;
        _threshold = thresholdInPips / _digit;
    }

    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        var quotationList = quotations.ToList();
        var bufferOpen = quotationList[0].Ask;
        var bufferDateTime = quotationList[0].Start;
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

        //SaveThresholdBarsToJson(Items, @"D:\forex.Terminal.WinUI3.Logs\", "thresholdBars.json");
    }

    public override void Add(Quotation quotation)
    {
        var lastThresholdBar = Items[^1];

        switch (lastThresholdBar.Direction)
        {
            case Direction.Up:
                if (quotation.Ask > lastThresholdBar.Close)
                {
                    lastThresholdBar.End = quotation.End;
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
                        Start = quotation.Start,
                        End = quotation.End,
                        Threshold = quotation.Ask + _threshold,
                        Ask = quotation.Ask,
                        Bid = quotation.Bid
                    });
                }
                else
                {
                    lastThresholdBar.End = quotation.End;
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                break;
            case Direction.Down:
                if (quotation.Ask < lastThresholdBar.Close)
                {
                    lastThresholdBar.End = quotation.End;
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
                        Start = quotation.Start,
                        End = quotation.End,
                        Threshold = quotation.Ask - _threshold,
                        Ask = quotation.Ask,
                        Bid = quotation.Bid
                    });
                }
                else
                {
                    lastThresholdBar.End = quotation.End;
                    lastThresholdBar.Ask = quotation.Ask;
                    lastThresholdBar.Bid = quotation.Bid;
                }
                break;
            case Direction.NaN:
            default: throw new ArgumentOutOfRangeException($"{nameof(lastThresholdBar.Direction)}", @"Encountered a ThresholdBars with invalid direction.");
        }
    }

    //private void SaveThresholdBarsToJson(IEnumerable<ThresholdBar> thresholdBars, string folderPath, string fileName)
    //{
    //    var symbolName = _symbol.ToString();
    //    var fullFileName = $"{symbolName}_{fileName}";
    //    _fileService.Save(folderPath, fullFileName, thresholdBars);
    //}
}