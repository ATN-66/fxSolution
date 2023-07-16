using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum TradeType
{
    [Description("ORDER_TYPE_NaN")] NaN = -1,
    [Description("ORDER_TYPE_BUY")] Buy = 0,
    [Description("ORDER_TYPE_SELL")] Sell = 1,
}