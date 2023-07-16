using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum DealType
{
    [Description("DEAL_TYPE_BUY")] Buy = 0,
    [Description("DEAL_TYPE_SELL")] Sell = 1,
    [Description("DEAL_TYPE_BALANCE")] Balance = 2,
    [Description("DEAL_TYPE_CREDIT")] Credit = 3,
    [Description("DEAL_TYPE_CHARGE")] Charge = 4,
    [Description("DEAL_TYPE_CORRECTION")] Correction = 5,
    [Description("DEAL_TYPE_BONUS")] Bonus = 6,
    [Description("DEAL_TYPE_COMMISSION")] Commission = 7,
    [Description("DEAL_TYPE_COMMISSION_DAILY")] CommissionDaily = 8,
    [Description("DEAL_TYPE_COMMISSION_MONTHLY")] CommissionMonthly = 9,
    [Description("DEAL_TYPE_COMMISSION_AGENT_DAILY")] CommissionDailyAgent = 10,
    [Description("DEAL_TYPE_COMMISSION_AGENT_MONTHLY")] CommissionMonthlyAgent = 11,
    [Description("DEAL_TYPE_INTEREST")] InterestRate = 12,
    [Description("DEAL_TYPE_BUY_CANCELED")] BuyCanceled = 13,
    [Description("DEAL_TYPE_SELL_CANCELED")] SellCanceled = 14,
    [Description("DEAL_DIVIDEND")] DividendOperations = 15,
    [Description("DEAL_DIVIDEND_FRANKED")] FrankedDividendOperations = 16,
    [Description("DEAL_TAX")] TaxCharges = 17,
}
