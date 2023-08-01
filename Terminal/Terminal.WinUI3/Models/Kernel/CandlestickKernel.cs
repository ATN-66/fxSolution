/*+------------------------------------------------------------------+
  |                                           Terminal.WinUI3.AI.Data|
  |                                             CandlestickKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using Terminal.WinUI3.Models.Entities;

namespace Terminal.WinUI3.Models.Kernel;

public class CandlestickKernel : Kernel<Candlestick>
{
    public override void AddRange(IEnumerable<Quotation> quotations)
    {
        var groupedQuotations = quotations.GroupBy(q => new
        {
            q.Symbol,
            q.DateTime.Year,
            q.DateTime.Month,
            q.DateTime.Day,
            q.DateTime.Hour,
            q.DateTime.Minute
        });

        var candlesticks = groupedQuotations.Select(group => new Candlestick
        {
            Symbol = group.Key.Symbol,
            Ask = group.Last().Ask,
            Bid = group.Last().Bid,
            DateTime = new DateTime(group.Key.Year, group.Key.Month, group.Key.Day, group.Key.Hour, group.Key.Minute, 0),

            Minutes = new DateTime(group.Key.Year, group.Key.Month, group.Key.Day, group.Key.Hour, group.Key.Minute, 0).ElapsedMinutesFromJanuaryFirstOf1970(),
            Open = group.First().Ask,
            Close = group.Last().Ask,
            High = group.Max(tick => tick.Ask),
            Low = group.Min(tick => tick.Ask),
        }).ToList();

        Items.AddRange(candlesticks);
    }

    public override void Add(Quotation quotation)
    {
        if (Items[^1].DateTime.AddMinutes(1) > quotation.DateTime)
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
                DateTime = new DateTime(quotation.DateTime.Year, quotation.DateTime.Month, quotation.DateTime.Day, quotation.DateTime.Hour, quotation.DateTime.Minute, 0),

                Minutes = new DateTime(quotation.DateTime.Year, quotation.DateTime.Month, quotation.DateTime.Day, quotation.DateTime.Hour, quotation.DateTime.Minute, 0).ElapsedMinutesFromJanuaryFirstOf1970(),
                Open = quotation.Ask,
                Close = quotation.Ask,
                High = quotation.Ask,
                Low = quotation.Ask,
            };

            Items.Add(newCandlestick);
        }
    }
}