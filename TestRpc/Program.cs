using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagar;
using Microsoft.Extensions.DependencyInjection;
using TestRpc.App;
using TestRpc.IO;
using TestRpc.Runtime;

namespace TestRpc
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var clientToServer = new Pipe(PipeOptions.Default);
            var serverToClient = new Pipe(PipeOptions.Default);
            var clientConnection = new ConnectionContext(serverToClient.Reader, clientToServer.Writer);
            var serverConnection = new ConnectionContext(clientToServer.Reader, serverToClient.Writer);
            await Task.WhenAll(RunServer(serverConnection), RunClient(clientConnection));
        }

        private static async Task RunServer<TConnection>(TConnection connection) where TConnection : IDuplexPipe
        {
            var services = StartNew(connection, out var connectionHandler, out var runtimeClient);

            var activation = new Activation(new ActivationId(7), new PingPongGrain(), runtimeClient);
            var catalog = services.GetRequiredService<Catalog>();
            catalog.RegisterActivation(activation);

            await Task.WhenAll(
                runtimeClient.Run(CancellationToken.None),
                connectionHandler.Run(CancellationToken.None),
                activation.Run(CancellationToken.None));
        }

        private static ServiceProvider StartNew<TConnection>(
            TConnection connection,
            out ConnectionHandler connectionHandler,
            out RuntimeClient runtimeClient) where TConnection : IDuplexPipe
        {
            var chan = Channel.CreateUnbounded<Message>();
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .AddSingleton<Catalog>()
                .AddSingleton<ProxyFactory>()
                .AddSingleton(sp => ActivatorUtilities.CreateInstance<ConnectionHandler>(sp, connection, chan.Writer))
                .AddSingleton(sp => ActivatorUtilities.CreateInstance<RuntimeClient>(sp, chan.Reader))
                .AddSingleton<IRuntimeClient>(sp => sp.GetRequiredService<RuntimeClient>())
                .BuildServiceProvider();
            connectionHandler = services.GetRequiredService<ConnectionHandler>();
            runtimeClient = services.GetRequiredService<RuntimeClient>();
            return services;
        }

        private static async Task RunClient<TConnection>(TConnection connection) where TConnection : IDuplexPipe
        {
            var services = StartNew(connection, out var connectionHandler, out var runtimeClient);

            var activation = new Activation(new ActivationId(100), new PingPongGrain(), runtimeClient);
            var catalog = services.GetRequiredService<Catalog>();
            catalog.RegisterActivation(activation);

            await Task.WhenAll(
                runtimeClient.Run(CancellationToken.None),
                connectionHandler.Run(CancellationToken.None),
                activation.Run(CancellationToken.None),
                Task.Run(
                    async () =>
                    {
                        var factory = services.GetRequiredService<ProxyFactory>();
                        var proxy = factory.GetProxy<IPingPongGrain>(new ActivationId(7));
                        var result = await proxy.Echo("Hello from client!");
                        Console.WriteLine(result);
                    }));
        }
    }
}