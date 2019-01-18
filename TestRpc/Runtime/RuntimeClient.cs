using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<int, IInvokable> pendingRequests = new ConcurrentDictionary<int, IInvokable>();

        private int messageId;

        public RuntimeClient(ConnectionHandler connection, Catalog catalog, ChannelReader<Message> incomingMessages)
        {
            this.connection = connection;
            this.catalog = catalog;
            this.incomingMessages = incomingMessages;
        }

        public ValueTask SendRequest<TInvokable>(ActivationId activationId, TInvokable request) where TInvokable : IInvokable
        {
            var message = new Message
            {
                Direction = Direction.Request,
                Target = activationId,
                Body = request
            };
            var currentActivation = RuntimeActivationContext.CurrentActivation;
            if (currentActivation != null)
            {
                currentActivation.OnSendMessage(message);
            }
            else
            {
                message.MessageId = Interlocked.Increment(ref this.messageId);
                message.Source = default;
                this.pendingRequests[message.MessageId] = request;
            }

            return this.connection.SendMessage(message, CancellationToken.None);
        }

        public ValueTask SendResponse(ActivationId activationId, object response)
        {
            var message = new Message
            {
                Direction = Direction.Response,
                Target = activationId,
                Body = response
            };

            return this.connection.SendMessage(message, CancellationToken.None);
        }

        public async Task Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && await this.incomingMessages.WaitToReadAsync(cancellation))
            {
                while (this.incomingMessages.TryRead(out var message))
                {
                    var handleMessage = this.HandleMessage(message);
                    if (handleMessage.IsCompletedSuccessfully) continue;
                    await handleMessage;
                }
            }
        }

        public ValueTask HandleMessage(Message message)
        {
            if (message.Target == default)
            {
                var request = this.pendingRequests[message.MessageId];
                request.Result = message.Body;
                // TODO: async completion.
                return default;
            }
            else
            {
                var activation = this.catalog.GetActivation(message.Target);
                return activation.EnqueueMessage(message);
            }
        }
    }
}