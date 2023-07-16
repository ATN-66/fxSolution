﻿using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade.Enums;

public enum TradeServerReturnCode
{
    [Description("NaN")] NaN = 0,
    [Description("Requote")] ReQuote = 10004,
    [Description("Request rejected")] Reject = 10006,
    [Description("Request canceled by trader")] Cancel = 10007,
    [Description("OpeningOrder placed")] Placed = 10008,
    [Description("Request completed")] Done = 10009,
    [Description("Only part of the request was completed")] DonePartial = 10010,
    [Description("Request processing error")] Error = 10011,
    [Description("Request canceled by timeout")] Timeout = 10012,
    [Description("Invalid request")] Invalid = 10013,
    [Description("Invalid volume in the request")] InvalidVolume = 10014,
    [Description("Invalid price in the request")] InvalidPrice = 10015,
    [Description("Invalid stops in the request")] InvalidStops = 10016,
    [Description("Trade is disabled")] TradeDisabled = 10017,
    [Description("Market is closed")] MarketClosed = 10018,
    [Description("There is not enough money to complete the request")] NoMoney = 10019,
    [Description("Prices changed")] PriceChanged = 10020,
    [Description("There are no quotes to process the request")] PriceOff = 10021,
    [Description("Invalid order expiration date in the request")] InvalidExpiration = 10022,
    [Description("OpeningOrder state changed")] OrderChanged = 10023,
    [Description("Too frequent requests")] TooManyRequests = 10024,
    [Description("No changes in request")] NoChanges = 10025,
    [Description("Autotrading disabled by server")] ServerDisablesAt = 10026,
    [Description("Autotrading disabled by client terminal")] ClientDisablesAt = 10027,
    [Description("Request locked for processing")] Locked = 10028,
    [Description("OpeningOrder or position frozen")] Frozen = 10029,
    [Description("Invalid order filling tradeType")] InvalidFill = 10030,
    [Description("No connection with the trade server")] Connection = 10031,
    [Description("Position is allowed only for live accounts")] OnlyReal = 10032,
    [Description("The number of pending orders has reached the limit")] LimitOrders = 10033,
    [Description("The volume of orders and positions for the symbol has reached the limit")] LimitVolume = 10034,
    [Description("Incorrect or prohibited order tradeType")] InvalidOrder = 10035,
    [Description("Position with the specified POSITION_IDENTIFIER has already been closed")] PositionClosed = 10036,
    [Description("A close volume exceeds the current position volume")] InvalidCloseVolume = 10038,
    [Description("A close order already exists for a specified position")] CloseOrderExist = 10039,
    [Description("The number of open positions simultaneously present on an account can be limited by the server settings")] LimitPositions = 10040,
    [Description("The pending order activation request is rejected, the order is canceled")] RejectCancel = 10041,
    [Description("The request is rejected, because the 'Only long positions are allowed' rule is set for the symbol (POSITION_TYPE_BUY)")] LongOnly = 10042,
    [Description("The request is rejected, because the 'Only short positions are allowed' rule is set for the symbol (POSITION_TYPE_SELL)")] ShortOnly = 10043,
    [Description("The request is rejected, because the 'Only position closing is allowed' rule is set for the symbol")] CloseOnly = 10044,
    [Description("The request is rejected, because 'Position closing is allowed only by FIFO rule' flag is set for the trading account (ACCOUNT_FIFO_CLOSE=true)")] FifoClose = 10045,
    [Description("The request is rejected, because the 'Opposite positions on a single symbol are disabled' rule is set for the trading account")] HedgeProhibited = 10046
}