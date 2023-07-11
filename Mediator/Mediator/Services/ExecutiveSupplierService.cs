using Mediator.Contracts.Services;

namespace Mediator.Services;

public class ExecutiveSupplierService : IExecutiveSupplierService
{
    public Task StartAsync()
    {
        return Task.CompletedTask;
    }
}
