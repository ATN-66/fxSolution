using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Terminal.WinUI3.Models.Trade;

namespace Terminal.WinUI3.Messenger.AccountService;

public class OrderRequestMessage : AsyncRequestMessage<Order>
{
    public Symbol Symbol
    {
        get; set;
    }

    public OrderRequestMessage(Symbol symbol)
    {
        Symbol = symbol;
    }
}