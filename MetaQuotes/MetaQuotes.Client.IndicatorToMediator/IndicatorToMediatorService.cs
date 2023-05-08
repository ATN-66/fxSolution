/*+------------------------------------------------------------------+
  |                   MetaQuotes.Prototype.Indicator.PipeMethodCalls |
  |                                    IndicatorToMediatorService.cs |
  +------------------------------------------------------------------+*/

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Common.Entities;
using Environment = Common.Entities.Environment;

namespace MetaQuotes.Client.IndicatorToMediator;

//This class is called by Indicator.mq5 or console simulator
public static class IndicatorToMediatorService
{
    private static Client client;

    static IndicatorToMediatorService()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var assemblyName = new AssemblyName(args.Name).Name;
            const string librariesPath = @"C:\MT5\Libraries";
            var assemblyPath = Path.Combine(librariesPath, assemblyName + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
        };
    }

    public static void DeInit(int symbol, int reason)
    {
        _ = client.DeInitAsync((Symbol)symbol, (DeInitReason)reason);
    }

    public static string Init(int symbol, string datetime, int ask, int bid, int environment)
    {
        Administrator.Instance.Environment = (Environment)environment;
        client = new Client((Symbol)symbol) { EnableLogging = false };
        var result = client.InitAsync(symbol, datetime, ask, bid, environment).ConfigureAwait(false);
        return result;
    }
    
    public static string Tick(int symbol, string datetime, int ask, int bid)
    {
        var quotation = new Quotation((Symbol)symbol, ParseStringToDateTime(datetime), ask, bid);
        return client.AddQuotationToQueue(quotation);
    }

    private static DateTime ParseStringToDateTime(string datetime)
    {
        DateTime resultDateTime;
        if (DateTime.TryParse(datetime, out var parsedDateTime)) resultDateTime = parsedDateTime;
        else throw new ArgumentException("The provided date string could not be parsed into a valid DateTime.");
        if (resultDateTime.Kind != DateTimeKind.Utc) resultDateTime = resultDateTime.ToUniversalTime();
        return resultDateTime;
    }
}