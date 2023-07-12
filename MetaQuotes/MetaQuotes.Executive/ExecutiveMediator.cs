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

    public static string Pulse(string dateTime, int type, int code, string message)
    {
        return _executiveClient.Pulse(dateTime, type, code, message);
    }
}