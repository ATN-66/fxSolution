using Grpc.Core;
using Mediator.Server.Terminal.To.Mediator;
using Protos.Grpc;

const string Host = "localhost";
const int Port = 50051;

var server = new Server
{
    Services = { TerminalToMediatorServer.BindService(new TerminalToMediator()) },
    Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
};

server.Start();

Console.WriteLine("GreeterServer listening on port " + Port);
Console.WriteLine("Press any key to stop the server...");
Console.ReadKey();

server.ShutdownAsync().Wait();