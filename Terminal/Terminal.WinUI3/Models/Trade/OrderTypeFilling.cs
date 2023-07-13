using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;

internal enum OrderTypeFilling
{
    [Description("fill or kill. The order can be filled only in the specified volume. If the necessary volume is absent, the order will not be executed.")]
    OrderFillingFok,
    [Description("immediate or cancel. The order can be filled in the volume not exceeding the requested one. The unfilled part of the order is canceled.")]
    OrderFillingIoc,
    [Description("fill the order in the volume available in the market. The unfilled part of the volume remains in the order.")]
    OrderFillingReturn
}