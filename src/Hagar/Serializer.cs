using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar
{
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

        public T Deserialize(ref Reader reader)
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

        public void Deserialize(ref Reader reader, ref T result)
        {
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            _codec.Deserialize(ref reader, ref result);
        }
    }
}