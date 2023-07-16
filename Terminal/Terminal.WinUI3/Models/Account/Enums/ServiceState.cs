using System.ComponentModel;

namespace Terminal.WinUI3.Models.Account.Enums;

public enum ServiceState
{
    [Description("Off")] Off,
    [Description("open position")] ReadyToOpen,
    [Description("busy")] Busy,
    [Description("close position")] ReadyToClose
}