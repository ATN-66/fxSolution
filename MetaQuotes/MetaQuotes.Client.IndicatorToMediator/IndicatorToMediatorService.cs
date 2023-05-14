/*+------------------------------------------------------------------+
  |                   MetaQuotes.Prototype.Indicator.PipeMethodCalls |
  |                                    IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MetaQuotes.Client.IndicatorToMediator;

//This class is called by Indicator.mq5 or console simulator
public static class IndicatorToMediatorService
{
    private static Client client;

    static IndicatorToMediatorService()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        const string librariesPath = @"C:\MT5\Libraries";
        var assemblyPath = Path.Combine(librariesPath, assemblyName + ".dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    public static void DeInit(int symbol, int reason)
    {
        client.DeInit(symbol, reason);
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