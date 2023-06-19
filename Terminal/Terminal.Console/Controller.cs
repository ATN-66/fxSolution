///*+------------------------------------------------------------------+
//  |                                                 Terminal.Console |
//  |                                                    Controller.cs |
//  +------------------------------------------------------------------+*/

//using Google.Protobuf;

//namespace Terminal.Console;

//public class Controller
//{
//    private readonly TerminalToMediatorClient _terminalToMediatorClient;
//    private readonly CancellationTokenSource _cts = new();

//    public Controller(TerminalToMediatorClient terminalToMediatorClient)
//    {
//        _terminalToMediatorClient = terminalToMediatorClient;
//    }

//    public async Task RunAsync(CancellationToken cancellationToken)
//    {
//        cancellationToken.Register(() => _cts.Cancel());

//        var connected = false;
//        while (!connected && !cancellationToken.IsCancellationRequested)
//        {
//            connected = await TryInitializeAsync().ConfigureAwait(false);
//            if (connected)
//            {
//                System.Console.WriteLine("Successfully connected to Mediator.");
//            }
//            else
//            {
                
//                System.Console.WriteLine("Failed to connect to Mediator. Retrying in 5 seconds...");
//                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
//            }
//        }

//        // If needed, you can add more code here that should be executed after a successful connection
//        await Task.CompletedTask.ConfigureAwait(false);
//    }

//    public async Task<bool> DeInitializeAsync()
//    {
//        if(!Administrator.MediatorConnected)
//        {
//            return true;
//        }

//        System.Console.Write("Disconnecting from Mediator...");
//        var request = new Request { RequestMessage = "Goodbye" };
//        var response = await _terminalToMediatorClient.DeInitAsync(request).ConfigureAwait(false);
//        System.Console.WriteLine($"Mediator response: {response.ResponseMessage}. Reason: {response.ReasonMessage}");
//        var result = response.ResponseMessage == "Goodbye";
//        if (!result)
//        {
//            throw new Exception("There is a problem!");
//        }

//        Administrator.MediatorConnected = false;
//        return result;
//    }

//    public async Task<bool> TryInitializeAsync()
//    {
//        System.Console.Write("Connecting to Mediator...");
//        var request = new Request { RequestMessage = "Hello" };
//        var response = await _terminalToMediatorClient.InitAsync(request).ConfigureAwait(false);
//        System.Console.WriteLine($"Mediator response: {response.ResponseMessage}. Reason: {response.ReasonMessage}");
//        var result = response.ResponseMessage == "Hello";
//        Administrator.MediatorConnected = result;
//        return result;
//    }
//}