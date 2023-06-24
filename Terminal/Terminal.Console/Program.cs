/*+------------------------------------------------------------------+
  |                                                 Terminal.Console |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Ticksdata;

Console.WriteLine("Terminal simulator...");

try
{
    using var channel = GrpcChannel.ForAddress("http://localhost:50051");
    var client = new TicksDataProvider.TicksDataProviderClient(channel);
    var callOptions = new Grpc.Core.CallOptions(deadline: DateTime.UtcNow.Add(TimeSpan.FromSeconds(10)));

    var request = new GetTicksRequest
    {
        StartDateTime = Timestamp.FromDateTime(new DateTime(2023, 1, 1).ToUniversalTime()),
        EndDateTime = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
    };

    var response = client.GetTicksAsync(request, callOptions);
    foreach (var quotation in response.Quotations)
    {
        Console.WriteLine(quotation);
    }
}
catch (Grpc.Core.RpcException ex) when (ex.StatusCode is Grpc.Core.StatusCode.Unavailable or Grpc.Core.StatusCode.DeadlineExceeded)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("----------------------");
    Console.WriteLine(ex.InnerException.Message);
    Console.WriteLine("----------------------");
    Console.WriteLine(ex.InnerException.InnerException.Message);
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    Console.WriteLine("----------------------");
    //Console.WriteLine(e.InnerException.Message);
    //Console.WriteLine("----------------------");
    //Console.WriteLine(e.InnerException.InnerException.Message);
}

Console.WriteLine("End of the program. Press any key to exit ...");
Console.ReadKey();
return 1;



//var host = CreateHostBuilder(args).Build();
//using (var scope = host.Services.CreateScope())
//{
//    var cts = new CancellationTokenSource();

//    //var mediatorToTerminalServer = scope.ServiceProvider.GetRequiredService<MediatorToTerminalService>();
//    //var mediatorToTerminalServerTask = mediatorToTerminalServer.StartAsync();

//    //var traderService = scope.ServiceProvider.GetRequiredService<Controller>();
//    //var traderTask = Task.Run(() => traderService.RunAsync(cts.Token));

//    //var consoleService = scope.ServiceProvider.GetRequiredService<ConsoleService>();
//    //var consoleTask = Task.Run(() => consoleService.RunAsync(cts.Token));

//    //await Task.WhenAny(consoleTask).ConfigureAwait(false);//, mediatorToTerminalServerTask
//    cts.Cancel();
//}


//await host.StopAsync().ConfigureAwait(false);
//Console.WriteLine("End of the program. Press any key to exit ...");
//Console.ReadKey();
//return 1;

//static IHostBuilder CreateHostBuilder(string[] args)
//{
//    return Host.CreateDefaultBuilder(args)
//        .ConfigureServices((_, services) =>
//        {
//            //services.AddSingleton<MediatorToTerminalService>();
//            //services.AddSingleton<TerminalToMediatorClient>();
//            //services.AddSingleton<Controller>();
//            //services.AddSingleton<ConsoleService>();
//        });
//}