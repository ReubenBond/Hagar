using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Hagar;
using Hagar.Activators;
using Hagar.GeneratedCodeHelpers;
using Hagar.Invocation;
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
                .AddHagar(hagar =>
                {
                    hagar.AddAssembly(typeof(Program).Assembly);
                    hagar.AddISerializableSupport();
                })
                .AddSingleton<IActivator<Message>, PooledMessageActivator>()
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

                        while (true)
                        {
                            RuntimeActivationContext.CurrentActivation = activation;
                            await proxy.Ping();
                        }
                    }));
        }
    }

    /*
    [System.CodeDom.Compiler.GeneratedCodeAttribute("HagarGen", "0.2.4.0")]
    internal sealed class HandCrafted_Invokable_IPingPongGrain_Ping : global::Hagar.Invocation.Request
    {
        [global::System.NonSerializedAttribute]
        global::TestRpc.App.IPingPongGrain target;
        public HandCrafted_Invokable_IPingPongGrain_Ping()
        {
        }

        public override int ArgumentCount => 0;
        public override void SetTarget<TTargetHolder>(TTargetHolder holder) => this.target = holder.GetTarget<global::TestRpc.App.IPingPongGrain>();
        public override TTarget GetTarget<TTarget>() => (TTarget)this.target;
        public override void Dispose()
        {
            this.target = default(global::TestRpc.App.IPingPongGrain);
        }

        public override TArgument GetArgument<TArgument>(int index)
        {
            switch ((index))
            {
                default:
                    return HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, 0);
            }
        }

        public override void SetArgument<TArgument>(int index, in TArgument value)
        {
            switch ((index))
            {
                default:
                    HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, 0);
                    return;
            }
        }

        protected override global::System.Threading.Tasks.ValueTask InvokeInner() => this.target.Ping();
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("HagarGen", "0.2.4.0")]
    internal sealed class HandCrafted_Invokable_IPingPongGrain_Echo_String : global::Hagar.Invocation.Request<string>
    {
        [global::Hagar.IdAttribute(0U)]
        public string arg0;
        [global::System.NonSerializedAttribute]
        global::TestRpc.App.IPingPongGrain target;
        public HandCrafted_Invokable_IPingPongGrain_Echo_String()
        {
        }

        public override int ArgumentCount => 1;
        public override void SetTarget<TTargetHolder>(TTargetHolder holder) => this.target = holder.GetTarget<global::TestRpc.App.IPingPongGrain>();
        public override TTarget GetTarget<TTarget>() => (TTarget)this.target;
        public override void Dispose()
        {
            this.arg0 = default(string);
            this.target = default(global::TestRpc.App.IPingPongGrain);
        }

        public override TArgument GetArgument<TArgument>(int index)
        {
            switch ((index))
            {
                case 0:
                    return (TArgument)(object)this.arg0;
                default:
                    return HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, 0);
            }
        }

        public override void SetArgument<TArgument>(int index, in TArgument value)
        {
            switch ((index))
            {
                case 0:
                    this.arg0 = (string)(object)value;
                    return;
                default:
                    HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, 0);
                    return;
            }
        }

        protected override global::System.Threading.Tasks.ValueTask<string> InvokeInner() => this.target.Echo(this.arg0);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("HagarGen", "0.2.4.0")]
    internal sealed class HandCrafted_IPingPongGrain_Proxy : global::TestRpc.Runtime.ProxyBase, global::TestRpc.App.IPingPongGrain
    {
        public HandCrafted_IPingPongGrain_Proxy(global::TestRpc.Runtime.ActivationId id, global::TestRpc.Runtime.IRuntimeClient runtimeClient) : base(id, runtimeClient)
        {
        }

        public global::System.Threading.Tasks.ValueTask Ping()
        {
            var completion = global::Hagar.Invocation.ResponseCompletionSourcePool.Get<object>();
            var request = global::Hagar.Invocation.InvokablePool.Get<HandCrafted_Invokable_IPingPongGrain_Ping>();
            base.SendRequest(completion, request);
            return completion.AsVoidValueTask();
        }

        public global::System.Threading.Tasks.ValueTask<string> Echo(string arg0)
        {
            var completion = global::Hagar.Invocation.ResponseCompletionSourcePool.Get<string>();
            var request = global::Hagar.Invocation.InvokablePool.Get<HandCrafted_Invokable_IPingPongGrain_Echo_String>();
            request.arg0 = arg0;
            base.SendRequest(completion, request);
            return completion.AsValueTask();
        }
    }
    */
}