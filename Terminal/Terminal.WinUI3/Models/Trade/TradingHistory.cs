namespace Terminal.WinUI3.Models.Trade;

public class TradingHistory
{
    public List<HistoryDeal> HistoryDeals
    {
        get; set;
    }
    public List<HistoryOrder> HistoryOrders
    {
        get; set;
    }
}