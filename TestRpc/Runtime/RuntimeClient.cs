using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagar.Invocation;
using TestRpc.IO;

namespace TestRpc.Runtime
{
    internal class RuntimeClient : IRuntimeClient
    {
        // only one connection for now and no concurrency control.
        private readonly ConnectionHandler connection;
        private readonly Catalog catalog;
        private readonly ChannelReader<Message> incomingMessages;
        private readonly ConcurrentDictionary<int, IResponseCompletionSource> pendingRequests = new ConcurrentDictionary<int, IResponseCompletionSource>();

        private int messageId;

        public RuntimeClient(ConnectionHandler connection, Catalog catalog, ChannelReader<Message> incomingMessages)
        {
            this.connection = connection;
            this.catalog = catalog;
            this.incomingMessages = incomingMessages;
        }

        public void SendRequest(ActivationId activationId, IResponseCompletionSource completion, IInvokable body)
        {
            var message = MessagePool.Get();
            message.Direction = Direction.Request;
            message.Target = activationId;
            message.Body = body;

            var currentActivation = RuntimeActivationContext.CurrentActivation;
            if (currentActivation != null)
            {
                currentActivation.OnSendMessage(message, completion);
            }
            else
            {
                message.MessageId = Interlocked.Increment(ref this.messageId);
                message.Source = default;
                this.pendingRequests[message.MessageId] = completion;
            }

            this.connection.SendMessage(message);
        }

        public void SendResponse(int requestMessageId, ActivationId requestMessageSource, Response response)
        {
            var message = MessagePool.Get();
            message.MessageId = requestMessageId;
            message.Direction = Direction.Response;
            message.Target = requestMessageSource;
            message.Body = response;

            this.connection.SendMessage(message);
        }

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await this.incomingMessages.WaitToReadAsync(cancellation))
            {
                while (this.incomingMessages.TryRead(out var message))
                {
                    this.HandleMessage(message);
                }
            }
        }

        public void HandleMessage(Message message)
        {
            if (message.Target == default)
            {
                // Ensure the message is disposed upon leaving this scope.
                using var _ = message;

                if (!this.pendingRequests.TryRemove(message.MessageId, out var request))
                {
                    ThrowMessageNotFound(message);
                    return;
                }

                request.Complete((Response)message.Body);
            }
            else
            {
                var activation = this.catalog.GetActivation(message.Target);
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