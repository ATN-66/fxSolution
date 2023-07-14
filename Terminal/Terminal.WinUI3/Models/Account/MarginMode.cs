using System.ComponentModel;

namespace Terminal.WinUI3.Models.Account;

public enum MarginMode
{
    [Description("Unknown margin mode")]
    None = -1,
    [Description("Netting. Only one position can exist for one symbol")]
    AccountMarginModeRetailNetting = 0, // Used for the OTC markets to interpret positions in the "netting" mode (only one position can exist for one symbol). The margin is calculated based on the symbol type (SYMBOL_TRADE_CALC_MODE).
    [Description("Exchange")]
    AccountMarginModeExchange = 1,  // Used for the exchange markets. Margin is calculated based on the discounts specified in symbol settings. Discounts are set by the broker, but not less than the values set by the exchange.
    [Description("Hedging. Multiple positions can exist for one symbol")]
    AccountMarginModeRetailHedging = 2 // Used for the exchange markets where individual positions are possible (hedging, multiple positions can exist for one symbol). The margin is calculated based on the symbol type (SYMBOL_TRADE_CALC_MODE) taking into account the hedged margin (SYMBOL_MARGIN_HEDGED).
}
