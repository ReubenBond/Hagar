using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    internal static class RuntimeActivationContext
    {
        [ThreadStatic] internal static Activation currentActivation = null;

        public static Activation CurrentActivation { get => currentActivation; set => currentActivation = value; }
    }

    internal class Activation : ITargetHolder
    {
        private readonly object activation;
        private readonly IRuntimeClient runtimeClient;
        private readonly Dictionary<Type, object> extensions = new Dictionary<Type, object>();
        private readonly ChannelReader<Message> pending;
        private readonly ChannelWriter<Message> incoming;
        private readonly Dictionary<int, IResponseCompletionSource> callbacks = new Dictionary<int, IResponseCompletionSource>();
        private int nextMessageId;

        public Activation(GrainId id, object activation, IRuntimeClient runtimeClient)
        {
            this.activation = activation;
            this.GrainId = id;
            this.runtimeClient = runtimeClient;
            var channel = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions
                    {SingleWriter = false, SingleReader = true, AllowSynchronousContinuations = false});
            this.pending = channel.Reader;
            this.incoming = channel.Writer;
        }

        public GrainId GrainId { get; }

        public TTarget GetTarget<TTarget>() => (TTarget)this.activation;

        public TComponent GetComponent<TComponent>() => (TComponent)this.extensions[typeof(TComponent)];

        public void OnSendMessage(Message message, IResponseCompletionSource completion)
        {
            var id = message.MessageId = this.nextMessageId++;
            message.Source = this.GrainId;
            this.callbacks[id] = completion;
        }

        public bool TryEnqueueMessage(Message request)
        {
            return this.incoming.TryWrite(request);
        }

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await this.pending.WaitToReadAsync(cancellation))
            {
                while (this.pending.TryRead(out var message))
                {
                    HandleMessage(message);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleMessage(Message message)
        {
            switch (message.Direction)
            {
                case Direction.Request:
                    {
                        var invokable = (IInvokable)message.Body;
                        invokable.SetTarget(this);
                        var responseTask = invokable.Invoke();
                        if (responseTask.IsCompleted)
                        {
                            // Ensure the message is disposed upon leaving this scope.
                            using var _ = message;

                            this.runtimeClient.SendResponse(message.MessageId, message.Source, responseTask.Result);
                            return;
                        }

                        _ = HandleRequestAsync(runtimeClient, message, responseTask);
                        return;

                        static async ValueTask HandleRequestAsync(IRuntimeClient runtimeClient, Message message, ValueTask<Response> responseTask)
                        {
                            // Ensure the message is disposed upon leaving this scope.
                            using var _ = message;

                            var response = await responseTask;
                            runtimeClient.SendResponse(message.MessageId, message.Source, response);
                        }
                    }

                case Direction.Response:
                    {
                        // Ensure the message is disposed upon leaving this scope.
                        using var _ = message;

                        if (!this.callbacks.Remove(message.MessageId, out var completion))
                        {
                            ThrowMessageNotFound(message);
                            return;
                        }

                        completion.Complete((Response)message.Body);
                        return;
                    }

                default:
                    {
                        // Ensure the message is disposed upon leaving this scope.
                        using var _ = message;
                        ThrowArgumentOutOfRange();
                        return;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowMessageNotFound(Message message) => throw new InvalidOperationException($"No pending request for message {message}");
    }
}