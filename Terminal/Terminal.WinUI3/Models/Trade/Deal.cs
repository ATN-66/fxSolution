using System.ComponentModel;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class Deal
{
    [Description("Unique identifier for the deal")] public ulong Ticket { get; set; }
    [Description("TradeServerReturnCode")] public TradeServerReturnCode TradeServerReturnCode { get; set; } = TradeServerReturnCode.NaN;
    [Description("DealType")] public DealType DealType { get; set; }
    [Description("TimeType (GTC, Day, SPECIFIED, SPECIFIED_DAY)")] public TimeType TimeType { get; set; } = TimeType.NaN;
    [Description("OrderAction (Deal, Pending, SLTP, Modify, Remove, CloseBy)")] public OrderAction OrderAction { get; set; } = OrderAction.NaN;
    [Description("OrderFilling (FOK, IOC, Return)")] public OrderFilling OrderFilling { get; set; } = OrderFilling.NaN;
    [Description("The price at which to place the order")] public double Price { get; set; }
    [Description("Stop loss level of the order")] public double StopLoss { get; set; }
    [Description("Take Profit level of the order")] public double TakeProfit { get; set; }
    [Description("The Ask at which the order was placed")] public double Ask { get; set; }
    [Description("The Bid at which the order was placed")] public double Bid { get; set; }
    [Description("The volume of the order in lots")] public double Volume { get; set; }
    [Description("The time of openning position")] public DateTime Time { get; set; }
}