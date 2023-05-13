/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                               OrdersProcessor.cs |
  +------------------------------------------------------------------+*/

using Protos.Grpc;

namespace Mediator.Processors;

public class OrdersProcessor
{
    private readonly Administrator.Administrator _administrator;

    public OrdersProcessor(Administrator.Administrator administrator)
    {
        _administrator = administrator;
    }

    public Task<Response> DeInitAsync(Request request)
    {
        if (request.RequestMessage != "Goodbye") throw new Exception($"{nameof(request)}");
        _administrator.TerminalConnected = false;
        Console.WriteLine("Terminal was disconnected.");

        return Task.FromResult(new Response
        {
            ResponseMessage = "Goodbye",
            ReasonMessage = "Have a nice day!"
        });
    }

    public Task<Response> InitAsync(Request request)
    {
        if (request.RequestMessage != "Hello") throw new Exception($"{nameof(request)}");

        if (_administrator is { IndicatorsConnected: true, ExpertAdvisorConnected: true })
        {
            _administrator.TerminalConnected = true;
            Console.WriteLine("Terminal was connected.");

            return Task.FromResult(new Response
            {
                ResponseMessage = "Hello",
                ReasonMessage = "Let's start!"
            });
        }

        _administrator.TerminalConnected = false;
        Console.WriteLine("Terminal rejected.");

        return Task.FromResult(new Response
        {
            ResponseMessage = "Goodbye",
            ReasonMessage = "MetaQuotes.MT5 platform is OFF."
        });
    }
}