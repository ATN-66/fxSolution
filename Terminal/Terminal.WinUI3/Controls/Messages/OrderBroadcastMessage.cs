using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Trade;

namespace Terminal.WinUI3.Controls.Messages;

public class OrderBroadcastMessage : ValueChangedMessage<Order>
{
    public Symbol Symbol
    {
        get; set;
    }

    public BroadcastOperation Operation
    {
        get; set;
    }

    public OrderBroadcastMessage(Symbol symbol, BroadcastOperation operation, Order order) : base(order)
    {
        Symbol = symbol;
        Operation = operation;
    }
}