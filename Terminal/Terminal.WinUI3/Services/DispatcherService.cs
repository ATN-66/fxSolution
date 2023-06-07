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

    public async Task ExecuteOnUIThreadAsync(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            await _dispatcherQueue.EnqueueAsync(action, priority).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("Cannot access UI thread.");
        }
    }

    public bool IsDispatcherQueueHasThreadAccess => _dispatcherQueue.HasThreadAccess;
}