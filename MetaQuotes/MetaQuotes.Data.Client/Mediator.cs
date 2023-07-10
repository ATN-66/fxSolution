/*+------------------------------------------------------------------+
  |                                           MetaQuotes.Data.PipeClient |
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MetaQuotes.Data.Client;

//This class is called by Indicator.mq5 or console simulator
public static class Mediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";
    private static PipeClient _pipeClient;

    static Mediator()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        const string librariesPath = mt5lib;
        var assemblyPath = Path.Combine(librariesPath, assemblyName + ".dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    public static void DeInit(int symbol, int reason)
    {
        _pipeClient.DeInit(symbol, reason);
        _pipeClient.Dispose();
    }

    public static string Init(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        _pipeClient = new PipeClient(symbol, enableLogging: false);
        return Task.Run(() => _pipeClient.InitAsync(id, symbol, datetime, ask, bid, environment)).GetAwaiter().GetResult();
    }

    public static string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        return _pipeClient.Tick(id, symbol, datetime, ask, bid);
    }

    //public static string SetAccountInfo


}