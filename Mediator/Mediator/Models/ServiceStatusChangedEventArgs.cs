using Common.Entities;

namespace Mediator.Models;

public class ServiceStatusChangedEventArgs : EventArgs
{
    public ServiceStatus ServiceStatus
    {
        get;
    }

    public ServiceStatusChangedEventArgs(ServiceStatus serviceStatus)
    {
        ServiceStatus = serviceStatus;
    }
}