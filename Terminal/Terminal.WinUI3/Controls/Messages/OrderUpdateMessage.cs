using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Terminal.WinUI3.Controls.Messages;

public class OrderUpdateMessage : AsyncRequestMessage<bool>
{
    public Symbol Symbol;
    public UpdateOperation UpdateOperation;
    public double Price;

    public OrderUpdateMessage(Symbol symbol, UpdateOperation updateOperation, double price)
    {
        Symbol = symbol;
        UpdateOperation = updateOperation;
        Price = price;
    }
}