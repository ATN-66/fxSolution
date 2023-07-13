using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;
internal enum OrderType
{
    [Description("Market Buy order")]
    OrderTypeBuy,
    [Description("Market Sell order")]
    OrderTypeSell,
    [Description("Buy Limit pending order")]
    OrderTypeBuyLimit,
    [Description("Sell Limit pending order")]
    OrderTypeSellLimit,
    [Description("Buy Stop pending order")]
    OrderTypeBuyStop,
    [Description("Sell Stop pending order")]
    OrderTypeSellStop,
    [Description("Upon reaching the order price, a pending Buy Limit order is placed at the StopLimit price")]
    OrderTypeBuyStopLimit,
    [Description("Upon reaching the order price, a pending Sell Limit order is placed at the StopLimit price")]
    OrderTypeSellStopLimit,
    [Description("Order to close a position by an opposite one")]
    OrderTypeCloseBy
}
