/*+------------------------------------------------------------------+
  |                                       Mediator.Contracts.Services|
  |                                            IDispatcherService.cs |
  +------------------------------------------------------------------+*/

using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Mediator.Contracts.Services;

public interface IDispatcherService
{
    void Initialize(DispatcherQueue dispatcherQueue);
    Task ExecuteOnUIThreadAsync(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal);
    bool HasThreadAccess { get; }
}