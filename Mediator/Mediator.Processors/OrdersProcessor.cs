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

    public async Task<Response> DeInitAsync(Request request)
    {
        if (request.RequestMessage != "Goodbye") throw new Exception("Wrong request.");
        _administrator.TerminalIsON = false;
        Console.WriteLine("Terminal was disconnected!");
        return await Task.FromResult(new Response
        {
            ResponseMessage = "Goodbye",
            ReasonMessage = "Have a nice day!"
        });
    }

    public async Task<Response> InitAsync(Request request)
    {
        if (request.RequestMessage != "Hello") throw new Exception("Wrong request.");
        Response response;
        if (_administrator.IndicatorsIsON)//TODO: Expert Advisor
        {
            _administrator.TerminalIsON = true;
            response = new Response
            {
                ResponseMessage = "Hello",
                ReasonMessage = "Let's start!"
            };
            Console.WriteLine("Terminal connected!");
        }
        else
        {
            _administrator.TerminalIsON = false;
            response = new Response
            {
                ResponseMessage = "Goodbye",
                ReasonMessage = "MetaQuotes.MT5 platform is OFF."
            };
            Console.WriteLine("Terminal rejected.");
        }

        return await Task.FromResult(response);
    }
}