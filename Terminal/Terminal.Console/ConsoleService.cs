/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                                ConsoleService.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.Console;

public class ConsoleService
{
    private readonly Controller _controller;
    private readonly CancellationTokenSource _cts = new();

    public ConsoleService(Controller controller)
    {
        _controller = controller;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        System.Console.WriteLine("Type a command or type 'exit' to close the application.");

        cancellationToken.Register(() => _cts.Cancel());

        while (!cancellationToken.IsCancellationRequested)
        {
            var command = System.Console.ReadLine();

            switch (command?.ToLower())
            {
                case "exit":
                    _cts.Cancel();
                    return;
                case "clear":
                    System.Console.Clear();
                    break;
                case "init":
                    await _controller.TryInitializeAsync();
                    break;
                case "deinit":
                    await _controller.DeInitializeAsync();
                    break;
                default:
                    System.Console.WriteLine("Unknown command. Try again.");
                    break;
            }
        }
    }
}