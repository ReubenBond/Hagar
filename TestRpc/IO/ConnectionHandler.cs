using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using TestRpc.Runtime;

namespace TestRpc.IO
{
    internal sealed class ConnectionHandler
    {
        private readonly ChannelWriter<Message> outgoingWriter;
        private readonly ChannelReader<Message> outgoingReader;
        private readonly ConnectionContext connection;
        private readonly ChannelWriter<Message> incoming;
        private readonly SessionPool serializerSessionPool;
        private readonly Serializer<Message> messageSerializer;

        public ConnectionHandler(ConnectionContext connection, ChannelWriter<Message> received, SessionPool sessionPool, Serializer<Message> messageSerializer)
        {
            this.connection = connection;
            var outgoing = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                });
            this.outgoingWriter = outgoing.Writer;
            this.outgoingReader = outgoing.Reader;
            this.incoming = received;
            this.serializerSessionPool = sessionPool;
            this.messageSerializer = messageSerializer;
        }

        public Task Run(CancellationToken cancellation) => Task.WhenAll(this.SendPump(cancellation), this.ReceivePump(cancellation));

        public void SendMessage(Message message) => this.outgoingWriter.TryWrite(message);

        private async Task ReceivePump(CancellationToken cancellation)
        {
            using (var session = this.serializerSessionPool.GetSession())
            {
                var input = this.connection.Input;
                while (!cancellation.IsCancellationRequested)
                {
                    ReadResult result;
                    while (true)
                    {
                        if (!input.TryRead(out result)) result = await input.ReadAsync(cancellation);

                        if (result.IsCanceled) break;
                        if (result.Buffer.IsEmpty && result.IsCompleted) break;

                        var message = ReadMessage(result.Buffer, session, out var consumedTo);
                        session.PartialReset();
                        input.AdvanceTo(consumedTo);
                        if (!this.incoming.TryWrite(message)) await this.incoming.WriteAsync(message, cancellation);
                    }

                    if (result.IsCanceled) break;
                    if (result.Buffer.IsEmpty && result.IsCompleted) break;
                }
            }

            Message ReadMessage(ReadOnlySequence<byte> payload, SerializerSession session, out SequencePosition consumedTo)
            {
                var reader = new Reader(payload, session);
                var result = this.messageSerializer.Deserialize(ref reader);
                consumedTo = payload.GetPosition(reader.Position);
                return result;
            }
        }

        private async Task SendPump(CancellationToken cancellation)
        {
            using (var session = this.serializerSessionPool.GetSession())
            {
                while (!cancellation.IsCancellationRequested && await this.outgoingReader.WaitToReadAsync(cancellation))
                {
                    while (this.outgoingReader.TryRead(out var item))
                    {
                        WriteMessage(item, session);
                        if (item.Body is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        MessagePool.Return(item);

                        session.PartialReset();

                        var flushResult = await this.connection.Output.FlushAsync(cancellation);
                        if (flushResult.IsCanceled || flushResult.IsCompleted) return;
                    }
                }
            }

            void WriteMessage(Message message, SerializerSession session)
            {
                var writer = new Writer<PipeWriter>(this.connection.Output, session);
                this.messageSerializer.Serialize(ref writer, message);
            }
        }
    }
}