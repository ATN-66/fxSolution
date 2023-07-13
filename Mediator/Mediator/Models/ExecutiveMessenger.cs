/*+------------------------------------------------------------------+
  |                                                  Mediator.Models |
  |                                                 DataMessenger.cs |
  +------------------------------------------------------------------+*/

using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;

namespace Mediator.Models;

public class ExecutiveMessenger : IExecutiveMessenger
{
    private readonly IExecutiveProviderService _executiveProviderService;

    public ExecutiveMessenger(IExecutiveProviderService executiveProviderService)
    {
        _executiveProviderService = executiveProviderService;
    }

    public void DeInit(string dateTime)
    {
        _executiveProviderService.DeInitAsync(dateTime);
    }

    public Task<string> InitAsync(string datetime)
    {
        return _executiveProviderService.InitAsync(datetime);
    }

    public Task<string> PulseAsync(string dateTime, string type, string code, string ticket, string details)
    {
        return _executiveProviderService.PulseAsync(dateTime, type, code, ticket, details);
    }
}