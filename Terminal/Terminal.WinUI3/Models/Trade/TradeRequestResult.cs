using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;
internal record struct TradeRequestResult
{
    [Description("Return code of a trade server")] private uint retcode;
    [Description("Deal ticket,  if a deal has been performed. It is available for a trade operation of TRADE_ACTION_DEAL tradeType")] ulong deal;
    [Description("Order ticket, if a ticket has been placed. It is available for a trade operation of TRADE_ACTION_PENDING tradeType")] ulong order;
    [Description("Deal volume, confirmed by broker. It depends on the order filling tradeType")] double volume;
    [Description("Deal price, confirmed by broker. It depends on the deviation field of the trade request and/or on the trade operation")] double price;
    [Description("The current market Bid price (requote price)")] double bid;
    [Description("The current market Ask price (requote price)")] double ask;
    [Description("The broker comment to operation (by default it is filled by description of trade server return code)")] string comment;
    [Description("Request ID set by the terminal when sending to the trade server")] uint request_id;
    [Description("The code of the error returned by an external trading system. The use and types of these errors depend on the broker and the external trading system, to which trading operations are sent.")] int retcode_external; 
}
