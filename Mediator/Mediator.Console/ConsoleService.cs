/*+------------------------------------------------------------------+
  |                                                 Mediator.Console |
  |                                                ConsoleService.cs |
  +------------------------------------------------------------------+*/

namespace Mediator.Console;

public class ConsoleService
{
    private readonly CancellationTokenSource _cts = new();

    public Task RunAsync(CancellationToken cancellationToken)
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
                    return Task.CompletedTask;
                case "cls":
                    System.Console.Clear();
                    break;
                default:
                    System.Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }

        return Task.CompletedTask;
    }
}