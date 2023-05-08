/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                                    Controller.cs |
  +------------------------------------------------------------------+*/

using Protos.Grpc;

namespace Terminal.Console
{
    public class Controller
    {
        private readonly TerminalToMediatorClient _terminalToMediatorClient;
        private readonly CancellationTokenSource _cts = new();

        public Controller(TerminalToMediatorClient terminalToMediatorClient)
        {
            _terminalToMediatorClient = terminalToMediatorClient;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _cts.Cancel());

            var connected = false;
            while (!connected && !cancellationToken.IsCancellationRequested)
            {
                connected = await TryInitializeAsync();
                if (connected)
                {
                    Administrator.MediatorIsON = true;
                    System.Console.WriteLine("Successfully connected to Mediator.");
                }
                else
                {
                    Administrator.MediatorIsON = false;
                    System.Console.WriteLine("Failed to connect to Mediator. Retrying in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }

            // If needed, you can add more code here that should be executed after a successful connection
            await Task.CompletedTask;
        }

        public async Task<bool> DeInitializeAsync()
        {
            System.Console.Write("Disconnecting from Mediator...");
            var request = new Request { RequestMessage = "Goodbye" };
            var response = await _terminalToMediatorClient.DeInitAsync(request);
            System.Console.WriteLine($"Mediator response: {response.ResponseMessage}. Reason: {response.ReasonMessage}");
            return response.ResponseMessage == "Goodbye";
        }

        public async Task<bool> TryInitializeAsync()
        {
            System.Console.Write("Connecting to Mediator...");
            var request = new Request { RequestMessage = "Hello" };
            var response = await _terminalToMediatorClient.InitAsync(request);
            System.Console.WriteLine($"Mediator response: {response.ResponseMessage}. Reason: {response.ReasonMessage}");
            return response.ResponseMessage == "Hello";
        }
    }
}