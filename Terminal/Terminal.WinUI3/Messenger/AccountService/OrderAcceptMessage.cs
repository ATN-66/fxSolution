using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Trade;

namespace Terminal.WinUI3.Messenger.AccountService;

public class OrderAcceptMessage : ValueChangedMessage<Order>
{
    public Symbol Symbol
    {
        get; set;
    }

    public AcceptType Type
    {
        get; set;
    }

    public OrderAcceptMessage(Symbol symbol, AcceptType type, Order order) : base(order)
    {
        Symbol = symbol;
        Type = type;
    }
}