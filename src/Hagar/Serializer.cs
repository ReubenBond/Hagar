using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;

namespace Hagar
{
#warning TODO: Surrogate type support
#warning TODO: Replace Jenkins Hash
#warning TODO: Formalize TypeCodec format for CLR types
#warning TODO: Make TypeCodec version-tolerant
#warning TODO: Deferred deserialization fields (esp useful for RPC)
#warning TODO: Object-model parser

    public sealed class Serializer<T>
    {
        private readonly IFieldCodec<T> codec;
        private readonly Type expectedType;

        public Serializer(ITypedCodecProvider codecProvider)
        {
            this.expectedType = typeof(T);
            this.codec = HagarGeneratedCodeHelper.UnwrapService(null, codecProvider.GetCodec<T>());
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, T value) where TBufferWriter : IBufferWriter<byte>
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
}