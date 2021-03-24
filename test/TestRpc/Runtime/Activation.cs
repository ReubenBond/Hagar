using Hagar.Invocation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TestRpc.Runtime
{
    internal static class RuntimeActivationContext
    {
        [ThreadStatic] internal static Activation Value = null;

        public static Activation CurrentActivation { get => Value; set => Value = value; }
    }

    internal class Activation : ITargetHolder
    {
        private readonly object _activation;
        private readonly IRuntimeClient _runtimeClient;
        private readonly Dictionary<Type, object> _extensions = new();
        private readonly ChannelReader<Message> _pending;
        private readonly ChannelWriter<Message> _incoming;
        private readonly Dictionary<int, IResponseCompletionSource> _callbacks = new();
        private int _nextMessageId;

        public Activation(GrainId id, object activation, IRuntimeClient runtimeClient)
        {
            _activation = activation;
            GrainId = id;
            _runtimeClient = runtimeClient;
            var channel = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions
                { SingleWriter = false, SingleReader = true, AllowSynchronousContinuations = false });
            _pending = channel.Reader;
            _incoming = channel.Writer;
        }

        public GrainId GrainId { get; }

        public TTarget GetTarget<TTarget>() => (TTarget)_activation;

        public TComponent GetComponent<TComponent>() => (TComponent)_extensions[typeof(TComponent)];

        public void OnSendMessage(Message message, IResponseCompletionSource completion)
        {
            var id = message.MessageId = _nextMessageId++;
            message.Source = GrainId;
            _callbacks[id] = completion;
        }

        public bool TryEnqueueMessage(Message request) => _incoming.TryWrite(request);

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await _pending.WaitToReadAsync(cancellation))
            {
                while (_pending.TryRead(out var message))
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

                            _runtimeClient.SendResponse(message.MessageId, message.Source, responseTask.Result);
                            return;
                        }

                        HandleRequestAsync(_runtimeClient, message, responseTask);
                        return;

                        static async void HandleRequestAsync(IRuntimeClient runtimeClient, Message message, ValueTask<Response> responseTask)
                        {
                            try
                            {
                                // Ensure the message is disposed upon leaving this scope.
                                using var _ = message;

                                var response = await responseTask;
                                runtimeClient.SendResponse(message.MessageId, message.Source, response);
                            }
                            catch (Exception exception)
                            {
                                try
                                {
                                    runtimeClient.SendResponse(message.MessageId, message.Source, Response.FromException(exception));
                                }
                                catch (Exception innerException)
                                {
                                    _ = innerException;
                                    // log something
                                }
                            }
                        }
                    }

                case Direction.Response:
                    {
                        // Ensure the message is disposed upon leaving this scope.
                        using var _ = message;

                        if (!_callbacks.Remove(message.MessageId, out var completion))
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
        private static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowMessageNotFound(Message message) => throw new InvalidOperationException($"No pending request for message {message}");
    }
}