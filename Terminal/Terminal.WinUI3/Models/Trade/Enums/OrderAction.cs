using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum OrderAction
{
    [Description("TRADE_ACTION_NaN")] NaN = -1, // Not a number
    [Description("TRADE_ACTION_DEAL")] Deal = 0, // Place a trade order for an immediate execution with the specified parameters (market order)
    [Description("TRADE_ACTION_PENDING")] Pending = 1, // Place a trade order for the execution under specified conditions (pending order)
    [Description("TRADE_ACTION_SLTP")] SLTP = 2, // Modify Stop Loss and Take Profit values of an opened position
    [Description("TRADE_ACTION_MODIFY")] Modify = 3, // Modify the parameters of the order placed previously
    [Description("TRADE_ACTION_REMOVE")] Remove = 4, // Delete the pending order placed previously
    [Description("TRADE_ACTION_CLOSE_BY")] CloseBy = 5 // Close a position by an opposite one
}