/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Administrator.cs |
  +------------------------------------------------------------------+*/

using Environment = Common.Entities.Environment;

namespace MetaQuotes.Client.IndicatorToMediator;

public class Administrator
{
    public Environment Environment;

    private static readonly object SyncRoot = new();
    private static volatile Administrator _instance;
    public static Administrator Instance
    {
        get
        {
            if (_instance != null) return _instance;
            lock (SyncRoot) _instance = new Administrator();
            return _instance;
        }
    }
}