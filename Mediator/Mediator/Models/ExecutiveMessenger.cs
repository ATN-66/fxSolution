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

    public string Pulse(string dateTime, int type, int code, string message)
    {
        return _executiveProviderService.Pulse(dateTime, type, code, message);
    }
}