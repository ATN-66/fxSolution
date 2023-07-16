using System.ComponentModel;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class ClosingOrder
{
    public ClosingOrder(ulong ticketToClose, ulong deviation)
    {
        TicketToClose = ticketToClose;
        Deviation = deviation;
    }

    [Description("Unique identifier for the order")] public ulong Ticket { get; set; }
    [Description("Unique identifier for the order to close")] public ulong TicketToClose { get; }
    [Description("Maximal possible deviation from the requested price")] public ulong Deviation { get; }
    [Description("TradeType (Buy, Sell)")] public TradeType TradeType { get; set; }
}