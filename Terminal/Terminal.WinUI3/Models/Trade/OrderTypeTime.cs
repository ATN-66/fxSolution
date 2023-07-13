using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;
internal enum OrderTypeTime
{
    [Description("Good till cancel order")]
    OrderTimeGtc,
    [Description(" Good till current trade day order")]
    OrderTimeDay,
    [Description("Good till expired order")]
    OrderTimeSpecified,
    [Description("The order will be effective till 23:59:59 of the specified day.If this time is outside a trading session, the order expires in the nearest trading time.")]
    OrderTimeSpecifiedDay
}
