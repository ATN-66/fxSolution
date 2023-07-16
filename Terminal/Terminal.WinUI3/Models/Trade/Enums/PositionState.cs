using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum PositionState
{
    [Description("Nothing")] NaN = -1,
    [Description("Position is pending to be opened")] ToBeOpened = 0,
    [Description("Position is opened")] Opened = 1,
    [Description("Position is pending to be closed")] ToBeClosed = 2,
    [Description("Position is closed")] Closed = 3,
    [Description("Position rejected to be opened")] RejectedToBeOpened = 4
}