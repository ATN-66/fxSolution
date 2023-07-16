using Common.Entities;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class HistoryPosition
{
    public HistoryPosition(long ticket)
    {
        Ticket = ticket;
    }

    public long Ticket
    {
        get; set;
    }

    public bool IsProfitable => EndDeal.Profit > 0;

    public string Symbol => StartDeal.Symbol;

    public string Type => ((DealType)StartDeal.Type).ToString();

    public string Start => (StartDeal.Time).ToString();


    public HistoryOrder StartOrder
    {
        get; set;
    }
    public HistoryDeal StartDeal
    {
        get; set;
    }

    public HistoryOrder EndOrder
    {
        get; set;
    }
    public HistoryDeal EndDeal
    {
        get; set;
    }
}