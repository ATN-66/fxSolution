using System.ComponentModel;

namespace Terminal.WinUI3.Models.Account;

internal enum StopOutMode
{
    [Description("Unknown stopout mode")]
    None = -1,
    [Description("Level is specified in percentage")]
    Percent = 0,
    [Description("Level is specified in money")]
    Money = 1 
}
