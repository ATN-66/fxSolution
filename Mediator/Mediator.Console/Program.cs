/*+------------------------------------------------------------------+
  |                                                 Mediator.Console |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.MetaQuotes.Mediator;
using Mediator.Administrator;
using Mediator.Client.Mediator.To.Terminal;
using Mediator.Console;
using Mediator.Processors;
using Mediator.Repository;
using Mediator.Service.Indicator.To.Mediator;
using Mediator.Service.Terminal.To.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Mediator...");

var host = CreateHostBuilder(args).Build();
using (var scope = host.Services.CreateScope())
{
    var cts = new CancellationTokenSource();

    var consoleService = scope.ServiceProvider.GetRequiredService<ConsoleService>();
    var consoleTask = Task.Run(() => consoleService.RunAsync(cts.Token));

    var indicatorToMediatorTasks = (from Symbol symbol in Enum.GetValues(typeof(Symbol))
                                    let serviceIndicatorToMediator = scope.ServiceProvider.GetService<IServiceIndicatorToMediator>()
                                    select Task.Run(() => serviceIndicatorToMediator.StartAsync(symbol, cts.Token))).ToList();

    var terminalToMediatorServer = scope.ServiceProvider.GetRequiredService<TerminalToMediatorService>();
    var terminalToMediatorServerTask = terminalToMediatorServer.StartAsync(cts.Token);

    var mediatorToTerminalClient = scope.ServiceProvider.GetRequiredService<Client>();

    var administrator = scope.ServiceProvider.GetRequiredService<Settings>();
    administrator.TerminalConnectedChanged += async (_, _) => { await mediatorToTerminalClient.StartAsync(cts.Token).ConfigureAwait(false); };
    administrator.TerminalConnectedChanged += async (_, _) => { await mediatorToTerminalClient.ProcessAsync(cts.Token).ConfigureAwait(false); };

    await Task.WhenAny(consoleTask, Task.WhenAll(indicatorToMediatorTasks), terminalToMediatorServerTask).ConfigureAwait(false);
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
            services.AddSingleton<Settings>();
            services.AddSingleton<IMSSQLRepository, MSSQLRepository>();
            services.AddTransient<IServiceIndicatorToMediator, Service>();
            services.AddSingleton<TerminalToMediatorService>();
            services.AddSingleton<Client>();
            services.AddSingleton<QuotationsProcessor>();
            services.AddSingleton<OrdersProcessor>();
            services.AddSingleton<ConsoleService>();
        });
}