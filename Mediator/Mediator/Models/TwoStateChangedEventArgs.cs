
namespace Mediator.Models;

public class TwoStateChangedEventArgs : EventArgs
{
    public bool IsActivated
    {
        get;
    }

    public TwoStateChangedEventArgs(bool isActivated)
    {
        IsActivated = isActivated;
    }
}