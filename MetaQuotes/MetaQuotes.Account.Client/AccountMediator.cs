/*+------------------------------------------------------------------+
  |                                        MetaQuotes.Account.Client |
  |                                               AccountMediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;

namespace MetaQuotes.Account.Client;

//This class is called by fxSolution.mq5 or console simulator
public static class AccountMediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";

    static AccountMediator()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyPath = Path.Combine(mt5lib, assemblyName + ".dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    public static string Init(string hello)
    {
        return hello;
    }
}