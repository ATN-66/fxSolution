/*+------------------------------------------------------------------+
  |                                                Mediator.Services |
  |                                             DispatcherService.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.WinUI;
using Mediator.Contracts.Services;
using Microsoft.UI.Dispatching;

namespace Mediator.Services;

public class DispatcherService : IDispatcherService
{
    private DispatcherQueue _dispatcherQueue = null!;

    public void Initialize(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public Task ExecuteOnUIThreadAsync(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return _dispatcherQueue.EnqueueAsync(action, priority);
    }

    public bool HasThreadAccess => _dispatcherQueue.HasThreadAccess;
}