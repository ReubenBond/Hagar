using Hagar.Buffers;
using Hagar.Buffers.Adaptors;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.IO;

namespace Hagar
{
    /// <summary>
    /// Serializes and deserializes values.
    /// </summary>
    public sealed class Serializer 
    {
        private readonly SerializerSessionPool _sessionPool;
        private readonly ICodecProvider _codecProvider;

        public Serializer(SerializerSessionPool sessionPool, ICodecProvider codecProvider)
        {
            _sessionPool = sessionPool;
            _codecProvider = codecProvider;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into a new array.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <returns>A byte array containing the serialized value.</returns>
        public byte[] SerializeToArray<T>(T value, int sizeHint = 0)
        {
            using var buffer = new PooledArrayBufferWriter(sizeHint);
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(buffer, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();

            // Copy the result into a fresh array.
            var written = writer.Output.WrittenSpan;
            var result = new byte[written.Length];
            written.CopyTo(result);

            return result;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize<T>(T value, ref Memory<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize<T>(T value, ref Memory<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize<T>(T value, Stream destination, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                var codec = _codecProvider.GetCodec<T>();
                codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                var codec = _codecProvider.GetCodec<T>();
                codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize<T>(T value, Stream destination, SerializerSession session, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                var writer = Writer.Create(buffer, session);
                var codec = _codecProvider.GetCodec<T>();
                codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                var writer = Writer.Create(buffer, session);
                var codec = _codecProvider.GetCodec<T>();
                codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<T, TBufferWriter>(T value, TBufferWriter destination) where TBufferWriter : IBufferWriter<byte>
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        public void Serialize<T, TBufferWriter>(T value, TBufferWriter destination, SerializerSession session) where TBufferWriter : IBufferWriter<byte>
        {
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<T, TBufferWriter>(T value, ref Writer<TBufferWriter> destination) where TBufferWriter : IBufferWriter<byte>
        {
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref destination, 0, typeof(T), value);
            destination.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize<T>(T value, ref Span<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize<T>(T value, ref Span<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize<T>(T value, byte[] destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize<T>(T value, byte[] destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(Stream source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(Stream source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlySequence<byte> source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlySequence<byte> source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlySpan<byte> source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlySpan<byte> source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(byte[] source) => Deserialize<T>(source.AsSpan());

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(byte[] source, SerializerSession session) => Deserialize<T>(source.AsSpan(), session);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> source) => Deserialize<T>(source.Span);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> source, SerializerSession session) => Deserialize<T>(source.Span, session);
        
        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The serialized type.</typeparam>
        /// <typeparam name="TInput">The reader input type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T, TInput>(ref Reader<TInput> source)
        {
            var codec = _codecProvider.GetCodec<T>();
            var field = source.ReadFieldHeader();
            return codec.ReadValue(ref source, field);
        }
    }

    /// <summary>
    /// Serializes and deserializes values.
    /// </summary>
    /// <typeparam name="T">The type of value which this instance serializes and deserializes.</typeparam>
    public sealed class Serializer<T>
    {
        private readonly IFieldCodec<T> _codec;
        private readonly SerializerSessionPool _sessionPool;
        private readonly Type _expectedType;

        public Serializer(ITypedCodecProvider codecProvider, SerializerSessionPool sessionPool)
        {
            _expectedType = typeof(T);
            _codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetCodec<T>());
            _sessionPool = sessionPool;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<TBufferWriter>(T value, ref Writer<TBufferWriter> destination) where TBufferWriter : IBufferWriter<byte>
        {
            _codec.WriteField(ref destination, 0, _expectedType, value);
            destination.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<TBufferWriter>(T value, TBufferWriter destination) where TBufferWriter : IBufferWriter<byte>
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        public void Serialize<TBufferWriter>(T value, TBufferWriter destination, SerializerSession session) where TBufferWriter : IBufferWriter<byte>
        {
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into a new array.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <returns>A byte array containing the serialized value.</returns>
        public byte[] SerializeToArray(T value, int sizeHint = 0)
        {
            using var buffer = new PooledArrayBufferWriter(sizeHint);
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(buffer, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();

            // Copy the result into a fresh array.
            var written = writer.Output.WrittenSpan;
            var result = new byte[written.Length];
            written.CopyTo(result);

            return result;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(T value, ref Memory<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(T value, ref Memory<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(T value, ref Span<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(T value, ref Span<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize(T value, byte[] destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize(T value, byte[] destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.WriteField(ref writer, 0, typeof(T), value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize(T value, Stream destination, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                _codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                _codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize(T value, Stream destination, SerializerSession session, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                var writer = Writer.Create(buffer, session);
                _codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                var writer = Writer.Create(buffer, session);
                _codec.WriteField(ref writer, 0, typeof(T), value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="TInput">The reader input type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<TInput>(ref Reader<TInput> source)
        {
            var field = source.ReadFieldHeader();
            return _codec.ReadValue(ref source, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(Stream source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(Stream source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlySequence<byte> source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlySequence<byte> source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlySpan<byte> source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlySpan<byte> source, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(byte[] source) => Deserialize(source.AsSpan());

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(byte[] source, SerializerSession session) => Deserialize(source.AsSpan(), session);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlyMemory<byte> source) => Deserialize(source.Span);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize(ReadOnlyMemory<byte> source, SerializerSession session) => Deserialize(source.Span, session);
    }

    /// <summary>
    /// Serializes and deserializes value types.
    /// </summary>
    /// <typeparam name="T">The type which this instance operates on.</typeparam>
    public sealed class ValueSerializer<T> where T : struct
    {
        private readonly IValueSerializer<T> _codec;
        private readonly SerializerSessionPool _sessionPool;
        private readonly Type _expectedType;

        public ValueSerializer(IValueSerializerProvider codecProvider, SerializerSessionPool sessionPool)
        {
            _sessionPool = sessionPool;
            _expectedType = typeof(T);
            _codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetValueSerializer<T>());
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<TBufferWriter>(ref T value, ref Writer<TBufferWriter> destination) where TBufferWriter : IBufferWriter<byte>
        {
            destination.WriteStartObject(0, _expectedType, _expectedType);
            _codec.Serialize(ref destination, ref value);
            destination.WriteEndObject();
            destination.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        public void Serialize<TBufferWriter>(ref T value, TBufferWriter destination) where TBufferWriter : IBufferWriter<byte>
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The output buffer writer.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        public void Serialize<TBufferWriter>(T value, TBufferWriter destination, SerializerSession session) where TBufferWriter : IBufferWriter<byte>
        {
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into a new array.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <returns>A byte array containing the serialized value.</returns>
        public byte[] SerializeToArray(ref T value, int sizeHint = 0)
        {
            using var buffer = new PooledArrayBufferWriter(sizeHint);
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(buffer, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();

            // Copy the result into a fresh array.
            var written = writer.Output.WrittenSpan;
            var result = new byte[written.Length];
            written.CopyTo(result);

            return result;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(ref T value, ref Memory<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(ref T value, ref Memory<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(ref T value, ref Span<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <remarks>This method slices the <paramref name="destination"/> to the serialized data length.</remarks>
        public void Serialize(ref T value, ref Span<byte> destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            destination = destination.Slice(0, writer.Position);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize(ref T value, byte[] destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The length of the serialized data.</returns>
        public int Serialize(ref T value, byte[] destination, SerializerSession session)
        {
            var writer = Writer.Create(destination, session);
            _codec.Serialize(ref writer, ref value);
            writer.Commit();
            return writer.Position;
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize(ref T value, Stream destination, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                _codec.Serialize(ref writer, ref value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                using var session = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, session);
                _codec.Serialize(ref writer, ref value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/> into <paramref name="destination"/>.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="destination">The destination where serialized data will be written.</param>
        /// <param name="session">The serializer session.</param>
        /// <param name="sizeHint">The estimated upper bound for the length of the serialized data.</param>
        /// <remarks>The destination stream will not be flushed by this method.</remarks>
        public void Serialize(ref T value, Stream destination, SerializerSession session, int sizeHint = 0)
        {
            if (destination is MemoryStream memoryStream)
            {
                var buffer = new MemoryStreamBufferWriter(memoryStream);
                var writer = Writer.Create(buffer, session);
                _codec.Serialize(ref writer, ref value);
                writer.Commit();
            }
            else
            {
                using var buffer = new PoolingStreamBufferWriter(destination, sizeHint);
                var writer = Writer.Create(buffer, session);
                _codec.Serialize(ref writer, ref value);
                writer.Commit();
            }
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="TInput">The reader input type.</typeparam>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize<TInput>(ref Reader<TInput> source, ref T result)
        {
            Field ignored = default;
            source.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref source, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(Stream source, ref T result)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(Stream source, ref T result, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlySequence<byte> source, ref T result)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlySequence<byte> source, ref T result, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlySpan<byte> source, ref T result)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlySpan<byte> source, ref T result, SerializerSession session)
        {
            var reader = Reader.Create(source, session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(byte[] source, ref T result) => Deserialize(source.AsSpan(), ref result);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(byte[] source, ref T result, SerializerSession session) => Deserialize(source.AsSpan(), ref result, session);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlyMemory<byte> source, ref T result) => Deserialize(source.Span, ref result);

        /// <summary>
        /// Deserialize a value of type <typeparamref name="T"/> from <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="result">The deserialized value.</param>
        /// <param name="session">The serializer session.</param>
        /// <returns>The deserialized value.</returns>
        public void Deserialize(ReadOnlyMemory<byte> source, ref T result, SerializerSession session) => Deserialize(source.Span, ref result, session);
    }
}