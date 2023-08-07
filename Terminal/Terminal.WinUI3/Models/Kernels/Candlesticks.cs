/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                  Candlesticks.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using Terminal.WinUI3.Models.Entities;

namespace Terminal.WinUI3.Models.Kernels;

public class Candlesticks : DataSourceKernel<Candlestick>
{
    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        var groupedQuotations = quotations.GroupBy(q => new
        {
            q.Symbol,
            q.Start.Year,
            q.Start.Month,
            q.Start.Day,
            q.Start.Hour,
            q.Start.Minute
        });

        var candlesticks = groupedQuotations.Select(group => new Candlestick
        {
            Symbol = group.Key.Symbol,
            Ask = group.Last().Ask,
            Bid = group.Last().Bid,
            Start = new DateTime(group.Key.Year, group.Key.Month, group.Key.Day, group.Key.Hour, group.Key.Minute, 0),

            Open = group.First().Ask,
            Close = group.Last().Ask,
            High = group.Max(tick => tick.Ask),
            Low = group.Min(tick => tick.Ask),
        }).ToList();

        Items.AddRange(candlesticks);
    }

    public override void Add(Quotation quotation)
    {
        if (Items[^1].Start.AddMinutes(1) > quotation.Start)
        {
            var lastCandle = Items[^1];
            lastCandle.Ask = quotation.Ask;
            lastCandle.Bid = quotation.Bid;

            if (quotation.Ask > lastCandle.High)
            {
                lastCandle.High = quotation.Ask;
            }

            if (quotation.Ask < lastCandle.Low)
            {
                lastCandle.Low = quotation.Ask;
            }

            lastCandle.Close = quotation.Ask;
        }
        else
        {
            var newCandlestick = new Candlestick
            {
                Symbol = quotation.Symbol,
                Ask = quotation.Ask,
                Bid = quotation.Bid,
                Start = new DateTime(quotation.Start.Year, quotation.Start.Month, quotation.Start.Day, quotation.Start.Hour, quotation.Start.Minute, 0),

                Open = quotation.Ask,
                Close = quotation.Ask,
                High = quotation.Ask,
                Low = quotation.Ask,
            };

            Items.Add(newCandlestick);
        }
    }
}