/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class Mediator : IMediator
{
    public Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime)
    {
        IEnumerable<Quotation> result = new List<Quotation>();
        return Task.FromResult(result);
    }
}
