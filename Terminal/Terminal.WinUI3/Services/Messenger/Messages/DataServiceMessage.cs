using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Terminal.WinUI3.Services.Messenger.Messages;

public abstract class DataServiceMessage<T> : ValueChangedMessage<T>
{
    protected DataServiceMessage(T value) : base(value)
    {
    }
}