/*+------------------------------------------------------------------+
  |                                               MetaQuotes.Account |
  |                                               AccountMediator.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;

namespace MetaQuotes.Account;

//This class is called by fxSolution.mq5 or console simulator
public static class AccountMediator
{
    private const string mt5lib = @"C:\forex.mt5\libraries";
    private static AccountClient _accountClient;

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

    public static string SetAccountInfo(long accountNumber)
    {
        _accountClient = new AccountClient();
        return _accountClient.SetAccountInfo(accountNumber);
    }
}