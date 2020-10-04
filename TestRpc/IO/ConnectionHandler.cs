using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TestRpc.Runtime;

namespace TestRpc.IO
{
    internal sealed class ConnectionHandler
    {
        private readonly ChannelWriter<Message> _outgoingWriter;
        private readonly ChannelReader<Message> _outgoingReader;
        private readonly ConnectionContext _connection;
        private readonly ChannelWriter<Message> _incoming;
        private readonly SessionPool _serializerSessionPool;
        private readonly Serializer<Message> _messageSerializer;

        public ConnectionHandler(ConnectionContext connection, ChannelWriter<Message> received, SessionPool sessionPool, Serializer<Message> messageSerializer)
        {
            _connection = connection;
            var outgoing = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                });
            _outgoingWriter = outgoing.Writer;
            _outgoingReader = outgoing.Reader;
            _incoming = received;
            _serializerSessionPool = sessionPool;
            _messageSerializer = messageSerializer;
        }

        public Task Run(CancellationToken cancellation) => Task.WhenAll(SendPump(cancellation), ReceivePump(cancellation));

        public void SendMessage(Message message) => _outgoingWriter.TryWrite(message);

        private async Task ReceivePump(CancellationToken cancellation)
        {
            using (var session = _serializerSessionPool.GetSession())
            {
                var input = _connection.Input;
                while (!cancellation.IsCancellationRequested)
                {
                    ReadResult result;
                    while (true)
                    {
                        if (!input.TryRead(out result))
                        {
                            result = await input.ReadAsync(cancellation);
                        }

                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (result.Buffer.IsEmpty && result.IsCompleted)
                        {
                            break;
                        }

                        var message = ReadMessage(result.Buffer, session, out var consumedTo);
                        session.PartialReset();
                        input.AdvanceTo(consumedTo);
                        if (!_incoming.TryWrite(message))
                        {
                            await _incoming.WriteAsync(message, cancellation);
                        }
                    }

                    if (result.IsCanceled)
                    {
                        break;
                    }

                    if (result.Buffer.IsEmpty && result.IsCompleted)
                    {
                        break;
                    }
                }
            }

            Message ReadMessage(ReadOnlySequence<byte> payload, SerializerSession session, out SequencePosition consumedTo)
            {
                var reader = Reader.Create(payload, session);
                var result = _messageSerializer.Deserialize(ref reader);
                consumedTo = payload.GetPosition(reader.Position);
                return result;
            }
        }

        private async Task SendPump(CancellationToken cancellation)
        {
            using (var session = _serializerSessionPool.GetSession())
            {
                while (!cancellation.IsCancellationRequested && await _outgoingReader.WaitToReadAsync(cancellation))
                {
                    while (_outgoingReader.TryRead(out var item))
                    {
                        WriteMessage(item, session);
                        if (item.Body is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        MessagePool.Return(item);

                        session.PartialReset();

                        var flushResult = await _connection.Output.FlushAsync(cancellation);
                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                        {
                            return;
                        }
                    }
                }
            }

            void WriteMessage(Message message, SerializerSession session)
            {
                var writer = Writer.Create(_connection.Output, session);
                _messageSerializer.Serialize(message, ref writer);
            }
        }
    }
}