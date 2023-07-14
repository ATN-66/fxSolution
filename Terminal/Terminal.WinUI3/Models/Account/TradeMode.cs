using System.ComponentModel;

namespace Terminal.WinUI3.Models.Account;

public enum TradeMode
{
    [Description("Unknown trade account")]
    None = -1,
    [Description("Demo trading account")]
    AccountTradeModeDemo = 0, 
    [Description("Contest trading account")]
    AccountTradeModeContest = 1,
    [Description("Real trading account")]
    AccountTradeModeReal = 2 
}
