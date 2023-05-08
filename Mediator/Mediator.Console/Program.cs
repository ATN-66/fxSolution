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
using Mediator.Repository.Interfaces;
using Mediator.Server.Indicator.To.Mediator;
using Mediator.Server.Terminal.To.Mediator;
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
                                    let server = scope.ServiceProvider.GetService<IIndicatorToMediatorServer>()
                                    select Task.Run(() => server.StartAsync(symbol, cts.Token))).ToList();

    var terminalToMediatorServer = scope.ServiceProvider.GetRequiredService<TerminalToMediatorServer>();
    var terminalToMediatorServerTask = terminalToMediatorServer.StartAsync(cts.Token);

    var mediatorToTerminalClient = scope.ServiceProvider.GetRequiredService<MediatorToTerminalClient>();

    var administrator = scope.ServiceProvider.GetRequiredService<Administrator>();
    administrator.TerminalIsONChanged += async (_, _) => { await mediatorToTerminalClient.StartAsync().ConfigureAwait(false); };
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
            services.AddSingleton<IIndicatorToMediatorServer, IndicatorToMediatorServer>();
            services.AddSingleton<TerminalToMediatorServer>();
            services.AddSingleton<MediatorToTerminalClient>();
            services.AddSingleton<Administrator>();
            services.AddSingleton<QuotationsProcessor>();
            services.AddSingleton<OrdersProcessor>();
            services.AddSingleton<IQuotationsRepository, MSSQLRepository>();
            services.AddSingleton<ConsoleService>();
        });
}