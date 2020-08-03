using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.WireProtocol;

namespace Hagar
{
    public sealed class Serializer<T>
    {
        private readonly IFieldCodec<T> codec;
        private readonly Type expectedType;

        public Serializer(ITypedCodecProvider codecProvider)
        {
            this.expectedType = typeof(T);
            this.codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetCodec<T>());
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, in T value) where TBufferWriter : IBufferWriter<byte>
        {
            this.codec.WriteField(ref writer, 0, this.expectedType, value);
            writer.Commit();
        }

        public T Deserialize(ref Reader reader)
        {
            var field = reader.ReadFieldHeader();
            return this.codec.ReadValue(ref reader, field);
        }
    }

    public sealed class ValueSerializer<T> where T : struct
    {
        private readonly IValueSerializer<T> codec;
        private readonly Type expectedType;

        public ValueSerializer(IValueSerializerProvider codecProvider)
        {
            this.expectedType = typeof(T);
            this.codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetValueSerializer<T>());
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, in T value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteStartObject(0, expectedType, expectedType);
            this.codec.Serialize(ref writer, in value);
            writer.WriteEndObject();
            writer.Commit();
        }

        public void Deserialize(ref Reader reader, ref T result)
        {
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            codec.Deserialize(ref reader, ref result);
        }
    }
}