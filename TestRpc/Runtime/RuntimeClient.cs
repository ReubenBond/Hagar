using Hagar.Invocation;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TestRpc.IO;

namespace TestRpc.Runtime
{
    internal class RuntimeClient : IRuntimeClient
    {
        // only one connection for now and no concurrency control.
        private readonly ConnectionHandler _connection;
        private readonly Catalog _catalog;
        private readonly ChannelReader<Message> _incomingMessages;
        private readonly ConcurrentDictionary<int, IResponseCompletionSource> _pendingRequests = new ConcurrentDictionary<int, IResponseCompletionSource>();

        private int _messageId;

        public RuntimeClient(ConnectionHandler connection, Catalog catalog, ChannelReader<Message> incomingMessages)
        {
            _connection = connection;
            _catalog = catalog;
            _incomingMessages = incomingMessages;
        }

        public void SendRequest(GrainId grainId, IResponseCompletionSource completion, IInvokable body)
        {
            var message = MessagePool.Get();
            message.Direction = Direction.Request;
            message.Target = grainId;
            message.Body = body;

            var currentActivation = RuntimeActivationContext.CurrentActivation;
            if (currentActivation != null)
            {
                currentActivation.OnSendMessage(message, completion);
            }
            else
            {
                message.MessageId = Interlocked.Increment(ref _messageId);
                message.Source = default;
                _pendingRequests[message.MessageId] = completion;
            }

            _connection.SendMessage(message);
        }

        public void SendResponse(int requestMessageId, GrainId requestMessageSource, Response response)
        {
            var message = MessagePool.Get();
            message.MessageId = requestMessageId;
            message.Direction = Direction.Response;
            message.Target = requestMessageSource;
            message.Body = response;

            _connection.SendMessage(message);
        }

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await _incomingMessages.WaitToReadAsync(cancellation))
            {
                while (_incomingMessages.TryRead(out var message))
                {
                    HandleMessage(message);
                }
            }
        }

        public void HandleMessage(Message message)
        {
            if (message.Target == default)
            {
                // Ensure the message is disposed upon leaving this scope.
                using var _ = message;

                if (!_pendingRequests.TryRemove(message.MessageId, out var request))
                {
                    ThrowMessageNotFound(message);
                    return;
                }

                request.Complete((Response)message.Body);
            }
            else
            {
                var activation = _catalog.GetActivation(message.Target);
                if (!activation.TryEnqueueMessage(message))
                {
                    // Ensure the message is disposed upon leaving this scope.
                    using var _ = message;
                    ThrowActivationCouldNotEnqueueMessage(activation, message);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowActivationCouldNotEnqueueMessage(Activation activation, Message message) => throw new InvalidOperationException($"Activation {activation} could not enqueue message {message}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowMessageNotFound(Message message) => throw new InvalidOperationException($"No pending request for message {message}");
    }
}