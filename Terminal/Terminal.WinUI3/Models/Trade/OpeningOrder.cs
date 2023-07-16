using System.ComponentModel;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class OpeningOrder
{
    public OpeningOrder(TradeType tradeType, ulong deviation, double freeMarginPercentToUse, double freeMarginPercentToRisk, string comment)
    {
        TradeType = tradeType;
        Deviation = deviation;
        FreeMarginPercentToUse = freeMarginPercentToUse;
        FreeMarginPercentToRisk = freeMarginPercentToRisk;
        Comment = comment;
    }

    [Description("Unique identifier for the order")] public ulong Ticket { get; set; }
    [Description("TradeType (Buy, Sell)")] public TradeType TradeType { get; }
    [Description("Maximal possible deviation from the requested price")] public ulong Deviation { get; }
    [Description("How many percent of Free Margin to use.")] public double FreeMarginPercentToUse { get; }
    [Description("How many percent of Free Margin trader is willing to lose.")] public double FreeMarginPercentToRisk { get; }
    [Description("A comment about the order")] public string Comment { get; }
}