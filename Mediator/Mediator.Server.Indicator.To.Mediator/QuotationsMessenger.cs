/*+------------------------------------------------------------------+
  |                            Mediator.Server.Indicator.To.Mediator |
  |                                           QuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

//The lifetime of this class is very short. It is instantiated by the Server and promptly discarded.

using Common.Entities;
using Common.MetaQuotes.Mediator;
using Mediator.Processors;

namespace Mediator.Server.Indicator.To.Mediator;

public class QuotationsMessenger : IQuotationsMessenger // , IDisposable
{
    //private static int _instanceCount;
    //private static int _lastInstanceId;
    private readonly QuotationsProcessor _quotationsProcessor;

    public QuotationsMessenger(QuotationsProcessor quotationsProcessor)
    {
        _quotationsProcessor = quotationsProcessor;
        //var instanceId = ++_lastInstanceId;
        //Interlocked.Increment(ref _instanceCount);
        //Console.WriteLine($"QuotationsMessenger instance {instanceId} created. Total instances: {_instanceCount}");
    }

    public void DeInit(Symbol symbol, DeInitReason reason)
    {
         _quotationsProcessor.DeInit(symbol, reason);
    }

    public string Init(int symbol, string datetime, double ask, double bid, int environment)
    {
        return _quotationsProcessor.Init(symbol, datetime, ask, bid, environment);
    }

    public string Tick(int symbol, string datetime, double ask, double bid)
    {
        return _quotationsProcessor.Tick(symbol, datetime, ask, bid);
    }

    //~QuotationsMessenger()
    //{
    //    //Dispose(false);
    //}

    //public void Dispose()
    //{
    //    //Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    //private void Dispose(bool disposing)
    //{
    //    //if (disposing)
    //    //{
    //    //    // Free any managed objects here if needed
    //    //}

    //    // Free any unmanaged objects here if needed
    //    //Interlocked.Decrement(ref _instanceCount);
    //    //Console.WriteLine($"QuotationsMessenger instance {_instanceId} disposed. Remaining instances: {_instanceCount}");
    //}
}