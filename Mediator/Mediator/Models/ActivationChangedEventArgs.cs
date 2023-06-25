

namespace Mediator.Models;

public class ActivationChangedEventArgs : EventArgs
{
    public bool IsActivated
    {
        get;
    }

    public ActivationChangedEventArgs(bool isActivated)
    {
        IsActivated = isActivated;
    }
}
