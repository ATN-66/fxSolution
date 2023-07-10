/*+------------------------------------------------------------------+
  |                                                MetaQuotes.Client |
  |                                                      Mediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MetaQuotes.Client;

//This class is called by Indicator.mq5 or console simulator
public static class Mediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";
    private static Client client;

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
        client.DeInit(symbol, reason);
        client.Dispose();
    }

    public static string Init(int id, int symbol, string datetime, double ask, double bid, int environment)
    {
        client = new Client(symbol, enableLogging: false);
        return Task.Run(() => client.InitAsync(id, symbol, datetime, ask, bid, environment)).GetAwaiter().GetResult();
    }

    public static string Tick(int id, int symbol, string datetime, double ask, double bid)
    {
        return client.Tick(id, symbol, datetime, ask, bid);
    }
}