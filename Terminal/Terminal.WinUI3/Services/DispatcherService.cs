/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                             DispatcherService.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

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

    public bool HasUIThreadAccess => _dispatcherQueue.HasThreadAccess;
}