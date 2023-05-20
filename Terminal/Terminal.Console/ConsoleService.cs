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
                    await _controller.DeInitializeAsync().ConfigureAwait(false);
                    _cts.Cancel();
                    return;
                case "clear":
                    System.Console.Clear();
                    break;
                case "init":
                    await _controller.TryInitializeAsync().ConfigureAwait(false);
                    break;
                case "deinit":
                    await _controller.DeInitializeAsync().ConfigureAwait(false);
                    break;
                default:
                    System.Console.WriteLine("Unknown command. Try again.");
                    break;
            }
        }
    }
}