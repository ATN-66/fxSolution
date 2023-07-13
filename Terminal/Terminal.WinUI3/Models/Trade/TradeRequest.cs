using System.ComponentModel;

namespace Terminal.WinUI3.Models.Trade;

internal struct TradeRequest
{
    [Description("Trade operation type")] TradeRequestAction action;
    [Description("Expert Advisor ID (magic number)")] ulong magic;
    [Description("Order ticket ")] ulong order;           
    [Description("Trade symbol ")] string symbol;          
    [Description("Requested volume for a deal in lots ")] double volume;          
    [Description("Price")] double price;            
    [Description("StopLimit level of the order ")] double stoplimit;       
    [Description("Stop Loss level of the order ")] double sl;              
    [Description("Take Profit level of the order ")] double tp;              
    [Description("Maximal possible deviation from the requested price ")] ulong deviation;       
    [Description("Order type ")] OrderType type;
    [Description("Order execution type ")] OrderTypeFilling type_filling;    
    [Description("Order expiration type ")] OrderTypeTime type_time;      
    [Description("Order expiration time (for the orders of ORDER_TIME_SPECIFIED type) ")] DateTime expiration;      
    [Description("Order comment ")] string comment;        
    [Description("Position ticket ")] ulong position;        
    [Description("The ticket of an opposite position ")] ulong position_by;      
}


