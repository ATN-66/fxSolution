using Common.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Terminal.WinUI3.Messenger.AccountService;

public class OrderModifyMessage : AsyncRequestMessage<bool>
{
    public readonly Symbol Symbol;
    public readonly double StopLoss;
    public readonly double TakeProfit;

    public OrderModifyMessage(Symbol symbol, double stopLoss, double takeProfit)
    {
        Symbol = symbol;
        StopLoss = stopLoss;
        TakeProfit = takeProfit;
    }
}