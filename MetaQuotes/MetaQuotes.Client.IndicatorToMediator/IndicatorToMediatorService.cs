/*+------------------------------------------------------------------+
  |                   MetaQuotes.Prototype.Indicator.PipeMethodCalls |
  |                                    IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Common.Entities;

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
        _ = client.DeInitAsync((Symbol)symbol, (DeInitReason)reason);
    }

    public static string Init(int symbol, string datetime, double ask, double bid, int environment)
    {
        client = new Client((Symbol)symbol, enableLogging: false);
        return Task.Run(() => client.InitAsync(symbol, datetime, ask, bid, environment)).GetAwaiter().GetResult();
    }

    public static string Tick(int symbol, string datetime, double ask, double bid)
    {
        return client.Add(symbol, datetime, ask, bid);
    }
}