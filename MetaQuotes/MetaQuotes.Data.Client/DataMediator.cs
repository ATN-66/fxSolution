/*+------------------------------------------------------------------+
  |                                           MetaQuotes.Data.Client |
  |                                                  DataMediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MetaQuotes.Data.Client;

//This class is called by fxSolution.mq5 or console simulator
public static class DataMediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";
    private static DataClient _dataClient;

    static DataMediator()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyPath = Path.Combine(mt5lib, assemblyName + ".dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    public static void DeInit(int symbol, int reason)
    {
        _dataClient.DeInit(symbol, reason);
        _dataClient.Dispose();
    }

    public static string Init(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        _dataClient = new DataClient(symbol, enableLogging: false);
        return Task.Run(() => _dataClient.InitAsync(id, symbol, datetime, ask, bid, environment)).GetAwaiter().GetResult();
    }

    public static string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        return _dataClient.Tick(id, symbol, datetime, ask, bid);
    }
}