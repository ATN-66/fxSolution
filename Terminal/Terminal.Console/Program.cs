/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Console;

Console.WriteLine("Terminal simulator...");

var host = CreateHostBuilder(args).Build();
using (var scope = host.Services.CreateScope())
{
    var cts = new CancellationTokenSource();

    var mediatorToTerminalServer = scope.ServiceProvider.GetRequiredService<MediatorToTerminalService>();
    var mediatorToTerminalServerTask = mediatorToTerminalServer.StartAsync();

    var traderService = scope.ServiceProvider.GetRequiredService<Controller>();
    var traderTask = Task.Run(() => traderService.RunAsync(cts.Token));

    var consoleService = scope.ServiceProvider.GetRequiredService<ConsoleService>();
    var consoleTask = Task.Run(() => consoleService.RunAsync(cts.Token));

    await Task.WhenAny(consoleTask, mediatorToTerminalServerTask).ConfigureAwait(false);
    cts.Cancel();
}


await host.StopAsync().ConfigureAwait(false);
Console.WriteLine("End of the program. Press any key to exit ...");
Console.ReadKey();
return 1;

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<MediatorToTerminalService>();
            services.AddSingleton<TerminalToMediatorClient>();
            services.AddSingleton<Controller>();
            services.AddSingleton<ConsoleService>();
        });
}