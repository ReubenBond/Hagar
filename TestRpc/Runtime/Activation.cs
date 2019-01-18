using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    internal static class RuntimeActivationContext
    {
        [ThreadStatic] internal static Activation currentActivation;

        public static Activation CurrentActivation => currentActivation;
    }

    internal class Activation : ITargetHolder
    {
        private readonly object activation;
        private readonly IRuntimeClient runtimeClient;
        private readonly Dictionary<Type, object> extensions = new Dictionary<Type, object>();
        private readonly ChannelReader<Message> pending;
        private readonly ChannelWriter<Message> incoming;
        private readonly Dictionary<int, Invokable> callbacks = new Dictionary<int, Invokable>();
        private int nextMessageId;

        public Activation(ActivationId id, object activation, IRuntimeClient runtimeClient)
        {
            this.activation = activation;
            this.ActivationId = id;
            this.runtimeClient = runtimeClient;
            var channel = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions
                    {SingleWriter = false, SingleReader = true, AllowSynchronousContinuations = false});
            this.pending = channel.Reader;
            this.incoming = channel.Writer;
        }

        public ActivationId ActivationId { get; }

        public TTarget GetTarget<TTarget>() => (TTarget)this.activation;

        public TExtension GetExtension<TExtension>() => (TExtension)this.extensions[typeof(TExtension)];

        public void OnSendMessage(Message message)
        {
            message.MessageId = this.nextMessageId++;
            message.Source = this.ActivationId;
        }

        public ValueTask EnqueueMessage(Message request)
        {
            if (this.incoming.TryWrite(request)) return default;

            return this.incoming.WriteAsync(request);
        }

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await this.pending.WaitToReadAsync(cancellation))
            {
                while (this.pending.TryRead(out var message))
                {
                    var result = HandleMessage(message);
                    if (result.IsCompletedSuccessfully) continue;
                    await result;
                }
            }
        }

        private ValueTask HandleMessage(Message message)
        {
            switch (message.Direction)
            {
                case Direction.Request:
                {
                    var invokable = (Invokable)message.Body;
                    invokable.SetTarget(this);
                    var resultTask = invokable.Invoke();
                    if (resultTask.IsCompletedSuccessfully)
                    {
                        return this.runtimeClient.SendResponse(message.Source, invokable.Result);
                    }

                    return HandleRequestAsync(resultTask);

                    async ValueTask HandleRequestAsync(ValueTask invokeTask)
                    {
                        await invokeTask;
                        await this.runtimeClient.SendResponse(message.Source, invokable.Result);
                    }
                }
                case Direction.Response:
                {
                    var invokable = this.callbacks[message.MessageId];
                    invokable.SetResult(message.Body);
                    // TODO: run continuation....
                    return default;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}