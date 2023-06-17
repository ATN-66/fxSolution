/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                               OrdersProcessor.cs |
  +------------------------------------------------------------------+*/

using Mediator.Contracts.Services;

namespace Mediator.Services;

public class OrdersProcessor : IOrdersProcessor
{
    //private readonly Administrator.Settings _settings;

    //public OrdersProcessor(Administrator.Settings settings)
    //{
    //    _settings = settings;
    //}

    //public Task<Response> DeInitAsync(Request request)
    //{
    //    if (request.RequestMessage != "Goodbye") throw new Exception($"{nameof(request)}");
    //    _settings.TerminalConnected = false;
    //    Console.WriteLine("Terminal was disconnected.");

    //    return Task.FromResult(new Response
    //    {
    //        ResponseMessage = "Goodbye",
    //        ReasonMessage = "Have a nice day!"
    //    });
    //}

    //public Task<Response> InitAsync(Request request)
    //{
    //    if (request.RequestMessage != "Hello") throw new Exception($"{nameof(request)}");

    //    if (_settings is { IndicatorsConnected: true, ExpertAdvisorConnected: true })
    //    {
    //        _settings.TerminalConnected = true;
    //        Console.WriteLine("Terminal was connected.");

    //        return Task.FromResult(new Response
    //        {
    //            ResponseMessage = "Hello",
    //            ReasonMessage = "Let's start!"
    //        });
    //    }

    //    _settings.TerminalConnected = false;
    //    Console.WriteLine("Terminal rejected.");

    //    return Task.FromResult(new Response
    //    {
    //        ResponseMessage = "Goodbye",
    //        ReasonMessage = "MetaQuotes.MT5 platform is OFF."
    //    });
    //}
}