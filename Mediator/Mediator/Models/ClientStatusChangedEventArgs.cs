
using Common.Entities;

namespace Mediator.Models;

public class ClientStatusChangedEventArgs : EventArgs
{
    public ClientStatus ClientStatus
    {
        get;
    }

    public ClientStatusChangedEventArgs(ClientStatus clientStatus)
    {
        ClientStatus = clientStatus;
    }
}