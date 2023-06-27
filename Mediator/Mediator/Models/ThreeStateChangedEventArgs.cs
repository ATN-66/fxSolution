namespace Mediator.Models;

public class ThreeStateChangedEventArgs : EventArgs
{
    public bool? IsActivated
    {
        get;
    }

    public ThreeStateChangedEventArgs(bool? isActivated)
    {
        IsActivated = isActivated;
    }
}