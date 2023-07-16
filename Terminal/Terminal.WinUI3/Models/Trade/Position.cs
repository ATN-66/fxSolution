using System.ComponentModel;
using Common.Entities;
using Terminal.WinUI3.Models.Trade.Enums;

namespace Terminal.WinUI3.Models.Trade;

public class Position
{
    public Position(Symbol symbol, TradeType tradeType, ulong deviation, double freeMarginPercentToUse, double freeMarginPercentToRisk, ulong magicNumber, string comment)
    {
        PositionState = PositionState.ToBeOpened;
        Symbol = symbol;
        TradeType = tradeType;
        MagicNumber = magicNumber;
        OpeningOrder = new OpeningOrder(TradeType, deviation, freeMarginPercentToUse, freeMarginPercentToRisk, comment);
    }

    public OpeningOrder OpeningOrder { get; set; }
    public ClosingOrder ClosingOrder { get; set; } = null!;
    public Deal OpeningDeal { get; set; } = null!;
    public Deal ClosingDeal { get; set; } = null!;

    [Description("PositionState (ToBeOpened, Opened, ToBeClosed, Closed, RejectedToBeOpened)")] public PositionState PositionState { get; set; }
    [Description("Unique identifier for the position")] public ulong Ticket { get; set; }
    [Description("The symbol of the security to operate")] public Symbol Symbol { get; }
    [Description("TradeType (Buy, Sell)")] public TradeType TradeType { get; }
    [Description("A unique identifier for the EA (magic number)")] public ulong MagicNumber { get; set; }
    [Description("Whether the position was profitable")] public bool IsProfitable => Profit > 0;
    [Description("profit/loss")] public double Profit { get; set; } = double.NaN;
}