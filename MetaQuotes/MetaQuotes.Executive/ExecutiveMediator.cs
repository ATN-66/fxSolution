/*+------------------------------------------------------------------+
  |                                             MetaQuotes.Executive |
  |                                             ExecutiveMediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MetaQuotes.Executive;

//This class is called by fxSolution.mq5 or console simulator
public static class ExecutiveMediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";
    private static ExecutiveClient _executiveClient;

    static ExecutiveMediator()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyPath = Path.Combine(mt5lib, assemblyName + ".dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    public static void DeInit(string dateTime)
    {
        _executiveClient.DeInit(dateTime);
        _executiveClient.Dispose();
    }

    public static string Init(string datetime)
    {
        _executiveClient = new ExecutiveClient();
        return Task.Run(() => _executiveClient.InitAsync(datetime)).GetAwaiter().GetResult();
    }

    public static string Pulse(string dateTime, string type, string code, string ticket, string result, string details)
    {
        return _executiveClient.Pulse(dateTime, type, code, ticket, result, details);
    }

    public static string AccountProperties(string dateTime, string type, string code, string ticket, string result, string details)
    {
        return _executiveClient.AccountProperties(dateTime, type, code, ticket, result, details);
    }

    public static string TradingHistory(string dateTime, string type, string code, string ticket, string result, string details)
    {
        return _executiveClient.TradingHistory(dateTime, type, code, ticket, result, details);
    }

    public static string MaxVolumes(string dateTime, string type, string code, string ticket, string result, string maxVolumes)
    {
        return _executiveClient.MaxVolumes(dateTime, type, code, ticket, result, maxVolumes);
    }

    public static string TickValues(string dateTime, string type, string code, string ticket, string result, string tickValues)
    {
        return _executiveClient.TickValues(dateTime, type, code, ticket, result, tickValues);
    }

    public static string UpdatePosition(string dateTime, string type, string code, string ticket, string result,  string details)
    {
        return _executiveClient.UpdatePosition(dateTime, type, code, ticket, result, details);
    }

    public static string UpdateTransaction(string dateTime, string type, string code, string ticket, string result, string details)
    {
        return _executiveClient.UpdateTransaction(dateTime, type, code, ticket, result, details);
    }
}