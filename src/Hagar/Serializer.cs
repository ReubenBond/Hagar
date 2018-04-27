using System;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;

namespace Hagar
{
#warning TODO: Surrogate type support
#warning TODO: Replace Jenkins Hash
#warning TODO: Formalize TypeCodec format for CLR types
#warning TODO: Make TypeCodec version-tolerant
#warning TODO: Deferred deserialization fields (esp useful for RPC)
#warning TODO: Object-model parser

    public class Serializer<T>
    {
        protected readonly ITypedCodecProvider CodecProvider;
        private readonly IFieldCodec<T> codec;
        private readonly Type expectedType = typeof(T);

        public Serializer(ITypedCodecProvider codecProvider)
        {
            this.CodecProvider = codecProvider;
            this.codec = codecProvider.GetCodec<T>();
        }

        public void Serialize(T value, SerializerSession session, Writer writer)
        {
            this.codec.WriteField(writer, session, 0, this.expectedType, value);
        }

        public T Deserialize(SerializerSession session, Reader reader)
        {
            var field = reader.ReadFieldHeader(session);
            return this.codec.ReadValue(reader, session, field);
        }
    }

    public class Serializer : Serializer<object>
    {
        public Serializer(ITypedCodecProvider codecProvider) : base(codecProvider)
        {
        }

        public void Serialize<T>(T value, SerializerSession session, Writer writer)
        {
            this.CodecProvider.GetCodec<T>().WriteField(writer, session, 0, typeof(T), value);
        }

        public T Deserialize<T>(SerializerSession session, Reader reader)
        {
            var field = reader.ReadFieldHeader(session);
            return this.CodecProvider.GetCodec<T>().ReadValue(reader, session, field);
        }
    }
}