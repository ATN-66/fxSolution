/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Messenger.Chart |
  |                                            CommunicationToken.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Messenger.Chart;

public abstract class CommunicationToken : IEquatable<CommunicationToken>
{
    public abstract bool Equals(CommunicationToken? other);

    public override bool Equals(object? obj)
    {
        return Equals(obj as CommunicationToken);
    }

    public override abstract int GetHashCode();
}
