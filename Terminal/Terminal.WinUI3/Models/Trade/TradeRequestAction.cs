using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;

internal enum TradeRequestAction
{
    [Description("Place a trade order for an immediate execution with the specified parameters (market order)")]
    TradeActionDeal,
    [Description("Place a trade order for the execution under specified conditions (pending order)")]
    TradeActionPending,
    [Description("Modify Stop Loss and Take Profit values of an opened position")]
    TradeActionSltp,
    [Description("Modify the parameters of the order placed previously")]
    TradeActionModify,
    [Description("Delete the pending order placed previously")]
    TradeActionRemove,
    [Description("Close a position by an opposite one")]
    TradeActionCloseBy
}
