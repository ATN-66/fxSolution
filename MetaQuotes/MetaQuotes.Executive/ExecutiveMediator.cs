/*+------------------------------------------------------------------+
  |                                             MetaQuotes.Executive |
  |                                               ExecutiveMediator.cs |
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

    public static string Pulse(string dateTime, string type, string code, string ticket, string details)
    {
        return _executiveClient.Pulse(dateTime, type, code, ticket, details);
    }

    public static string IntegerAccountProperties(string dateTime, string type, string code, string ticket, long login, int tradeMode, long leverage, int stopOutMode, int marginMode, bool tradeAllowed, bool tradeExpert, int limitOrders)
    {
        return _executiveClient.IntegerAccountProperties(dateTime, type, code, ticket, login, tradeMode, leverage, stopOutMode, marginMode, tradeAllowed, tradeExpert, limitOrders);
    }

    public static string DoubleAccountProperties(string dateTime, string type, string code, string ticket, double balance, double credit, double profit, double equity, double margin, double freeMargin, double marginLevel, double marginCall, double marginStopOut)
    {
        return _executiveClient.DoubleAccountProperties(dateTime, type, code, ticket, balance, credit, profit, equity, margin, freeMargin, marginLevel, marginCall, marginStopOut);
    }

    public static string StringAccountProperties(string dateTime, string type, string code, string ticket, string name, string server, string currency, string Company)
    {
        return _executiveClient.StringAccountProperties(dateTime, type, code, ticket, name, server, currency, Company);
    }

    public static string MaxVolumes(string dateTime, string type, string code, string ticket, string maxVolumes)
    {
        return _executiveClient.MaxVolumes(dateTime, type, code, ticket, maxVolumes);
    }

}
