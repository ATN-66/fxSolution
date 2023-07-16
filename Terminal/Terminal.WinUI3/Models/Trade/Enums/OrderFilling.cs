using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum OrderFilling
{
    [Description("ORDER_FILLING_NaN")] NaN = -1,
    [Description("ORDER_FILLING_FOK")] FOK = 0, // fill or kill. The order can be filled only in the specified volume. If the necessary volume is absent, the order will not be executed.
    [Description("ORDER_FILLING_IOC")] IOC = 1, // Immediate or cancel. The order can be filled in the volume not exceeding the requested one. The unfilled part of the order is canceled.
    [Description("ORDER_FILLING_IOC")] BOC = 2, // The BoC order assumes that the order can only be placed in the Depth of Market and cannot be immediately executed. If the order can be executed immediately when placed, then it is canceled.
    [Description("ORDER_FILLING_RETURN")] Return = 3 // Fill the order in the volume available in the market. The unfilled part of the volume remains in the order.
}