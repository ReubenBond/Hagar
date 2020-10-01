using Hagar.Buffers;
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
    public sealed class Serializer 
    {
        private readonly SessionPool _sessionPool;
        private readonly ICodecProvider _codecProvider;

        public byte[] SerializeToByteArray<T>(T value)
        {
            return default;
        }

        public T Deserialize<T>(byte[] source) => Deserialize<T>(new ReadOnlyMemory<byte>(source));
        
        public T Deserialize<T>(ReadOnlyMemory<byte> source)
        {
            var sequence = new ReadOnlySequence<byte>(source);
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(sequence, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }

        public void Serialize<T>(T value, Stream destination)
        {

        }

        public T Deserialize<T>(Stream source)
        {
            return default;
        }

        public void Serialize<T>(T value, IBufferWriter<byte> destination)
        {
            using var session = _sessionPool.GetSession();
            var writer = new Writer<IBufferWriter<byte>>(destination, session);
            var codec = _codecProvider.GetCodec<T>();
            codec.WriteField(ref writer, 0, typeof(T), value);
        }

        public T Deserialize<T>(ReadOnlySequence<byte> source)
        {
            using var session = _sessionPool.GetSession();
            var reader = Reader.Create(source, session);
            var codec = _codecProvider.GetCodec<T>();
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }
    }

    public sealed class Serializer<T>
    {
        private readonly IFieldCodec<T> _codec;
        private readonly Type _expectedType;

        public Serializer(ITypedCodecProvider codecProvider)
        {
            _expectedType = typeof(T);
            _codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetCodec<T>());
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, in T value) where TBufferWriter : IBufferWriter<byte>
        {
            _codec.WriteField(ref writer, 0, _expectedType, value);
            writer.Commit();
        }

        public T Deserialize<TInput>(ref Reader<TInput> reader)
        {
            var field = reader.ReadFieldHeader();
            return _codec.ReadValue(ref reader, field);
        }
    }

    public sealed class ValueSerializer<T> where T : struct
    {
        private readonly IValueSerializer<T> _codec;
        private readonly Type _expectedType;

        public ValueSerializer(IValueSerializerProvider codecProvider)
        {
            _expectedType = typeof(T);
            _codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetValueSerializer<T>());
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, in T value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteStartObject(0, _expectedType, _expectedType);
            _codec.Serialize(ref writer, in value);
            writer.WriteEndObject();
            writer.Commit();
        }

        public void Deserialize<TInput>(ref Reader<TInput> reader, ref T result)
        {
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }
    }
}