﻿using Common.Entities;
using Common.ExtensionsAndHelpers;

namespace Terminal.WinUI3.AI.Data;

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
        
    }
}