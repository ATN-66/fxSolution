using System.ComponentModel;

namespace Terminal.WinUI3.Models.Account;

public class AccountInfo
{
    // Group 1: General account information
    [Description("Account number")] public long Login { get; set; }
    [Description("Client name")] public string Name { get; set; }
    [Description("Account currency")] public string Currency { get; set; }
    [Description("Trade server name")] public string Server { get; set; }
    [Description("Name of a company that serves the account")] public string Company { get; set; }

    // Group 2: Trade settings
    [Description("Account trade mode")] public TradeMode TradeMode { get; set; }
    [Description("Allowed trade for the current account")] public bool TradeAllowed { get; set; }
    [Description("Allowed trade for an Expert Advisor")] public bool TradeExpert { get; set; }
    [Description("Maximum allowed number of active pending orders")] public int LimitOrders { get; set; }

    // Group 3: Account financial details
    [Description("Account balance in the deposit currency")] public double Balance { get; set; }
    [Description("Account credit in the deposit currency")] public double Credit { get; set; }
    [Description("Current profit of an account in the deposit currency")] public double Profit { get; set; }
    [Description("Account equity in the deposit currency")] public double Equity { get; set; }
    [Description("Account margin used in the deposit currency")] public double Margin { get; set; }
    [Description("Free margin of an account in the deposit currency")] public double FreeMargin { get; set; }

    // Group 4: Leverage and Margin settings
    [Description("Account leverage")] public long Leverage { get; set; }
    [Description("Account stop out mode")] public StopOutMode StopOutMode { get; set; }
    [Description("Account margin mode")] public MarginMode MarginMode { get; set; }
    [Description("Account margin level in percents")] public double MarginLevel { get; set; }
    [Description("Margin call level.")] public double MarginCall { get; set; }
    [Description("Margin stop out level")] public double MarginStopOut { get; set; }
}
