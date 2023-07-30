using System.ComponentModel;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class Order
{
    public Order(TradeType tradeType, string comment)
    {
        TradeType = tradeType;
        Comment = comment;
    }

    [Description("Unique identifier for the order")] public ulong Ticket { get; set; }
    [Description("TradeType (Buy, Sell)")] public TradeType TradeType { get; }
    [Description("A comment about the order")] public string Comment { get; }
    [Description("The price at which the order was placed")] public double Price { get; set; }
    [Description("Stop loss level of the order")] public double StopLoss { get; set; }
    [Description("Take Profit level of the order")] public double TakeProfit { get; set; }
    [Description("The Ask at which the order was placed")] public double Ask { get; set; }
    [Description("The Bid at which the order was placed")] public double Bid { get; set; }
    [Description("The volume of the order in lots")] public double Volume { get; set; }
    [Description("The time at which the order was placed")] public DateTime Time { get; set; }

    public TradeType OppositeTradeType => TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;

    public static readonly Order Null = new(TradeType.NaN, string.Empty);
}