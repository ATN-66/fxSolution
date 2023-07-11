/*+------------------------------------------------------------------+
  |                                Mediator.Services.PipeMethodCalls |
  |                                  DataMessenger.cs |
  +------------------------------------------------------------------+*/

// The lifetime of this class is very short. It is instantiated by the Server and promptly discarded.
// The problem resolved: a private static field in the server class to hold the instance of DataMessenger was created.

//using System.Diagnostics;
using Common.MetaQuotes.Mediator;
using Mediator.Contracts.Services;

namespace Mediator.Models;

public class DataMessenger : IDataMessenger//, IDisposable
{
    //private static int _instanceCount;
    //private static int _lastInstanceId;
    //private static int _instanceId;

    private readonly IDataProviderService _dataProviderService;

    public DataMessenger(IDataProviderService dataProviderService)
    {
        //_instanceId = ++_lastInstanceId;
        //Interlocked.Increment(ref _instanceCount);
        //Debug.WriteLine($"QuotationsMessenger instance {_instanceId} created. Total instances: {_instanceCount}");
        _dataProviderService = dataProviderService;
    }

    public void DeInit(int symbol, int reason)
    {
        _dataProviderService.DeInitAsync(symbol, reason);
    }

    public Task<string> InitAsync(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        return _dataProviderService.InitAsync(id, symbol, datetime, ask, bid, environment);
    }

    public string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        return _dataProviderService.Tick(id, symbol, datetime, ask, bid);
    }

    //~DataMessenger()
    //{
    //    Dispose(false);
    //}

    //public void Dispose()
    //{
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    //private void Dispose(bool disposing)
    //{
    //    if (disposing)
    //    {
    //        // Free any managed objects here if needed
    //    }

    //    //Free any unmanaged objects here if needed
    //    Interlocked.Decrement(ref _instanceCount);
    //    Console.WriteLine($@"QuotationsMessenger instance {_instanceId} disposed. Remaining instances: {_instanceCount}");
    //}
}