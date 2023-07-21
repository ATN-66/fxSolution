using System.ComponentModel;
using Common.Entities;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class Position
{
    public Position(Symbol symbol, TradeType tradeType, ulong deviation, ulong magicNumber, double freeMarginPercentToUse, double freeMarginPercentToRisk)
    {
        StartOrder = new Order(tradeType)
        {
            Comment = "Order opened by EXECUTIVE EA"
        };
        EndOrder = new Order(StartOrder.OppositeTradeType);

        Symbol = symbol;
        MagicNumber = magicNumber;
        Deviation = deviation;
        FreeMarginPercentToUse = freeMarginPercentToUse;
        FreeMarginPercentToRisk = freeMarginPercentToRisk;
        PositionState = PositionState.ToBeOpened;
    }

    [Description("Order to start position")] public Order StartOrder { get; }
    [Description("Order to end position")] public Order EndOrder { get; }

    [Description("PositionState (ToBeOpened, Opened, ToBeClosed, Closed, RejectedToBeOpened)")] public PositionState PositionState { get; set; }
    [Description("The symbol of the security to operate")] public Symbol Symbol { get; }
    [Description("Unique identifier for the position")] public ulong Ticket { get; set; }
    [Description("A unique identifier for the EA (magic number)")] public ulong MagicNumber { get; set; }
    [Description("Maximal possible deviation from the requested price")] public ulong Deviation { get; }
    [Description("How many percent of Free Margin to use.")] public double FreeMarginPercentToUse { get; }
    [Description("How many percent of Free Margin trader is willing to lose.")] public double FreeMarginPercentToRisk { get; }
}