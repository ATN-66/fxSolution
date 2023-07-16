using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum TimeType
{
    [Description("ORDER_TIME_NaN")] NaN = -1,
    [Description("ORDER_TIME_GTC")] GTC = 0, // Good till cancel order
    [Description("ORDER_TIME_DAY")] TimeDay = 1, // Good till current trade day order
    [Description("ORDER_TIME_SPECIFIED")] TimeSpecified = 2, // Good till expired order
    [Description("ORDER_TIME_SPECIFIED_DAY")] TimeSpecifiedDay = 3 // The order will be effective till 23:59:59 of the specified day.If this time is outside a trading session, the order expires in the nearest trading time.
}