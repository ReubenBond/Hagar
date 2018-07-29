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

        public void Serialize(T value, SerializerSession session, ref Writer writer)
        {
            this.codec.WriteField(ref writer, session, 0, this.expectedType, value);
            writer.Commit();
        }

        public T Deserialize(SerializerSession session, ref Reader reader)
        {
            var field = reader.ReadFieldHeader(session);
            return this.codec.ReadValue(ref reader, session, field);
        }
    }

    public class Serializer : Serializer<object>
    {
        public Serializer(ITypedCodecProvider codecProvider) : base(codecProvider)
        {
        }

        public void Serialize<T>(T value, SerializerSession session, ref Writer writer)
        {
            this.CodecProvider.GetCodec<T>().WriteField(ref writer, session, 0, typeof(T), value);
            writer.Commit();
        }

        public T Deserialize<T>(SerializerSession session, ref Reader reader)
        {
            var field = reader.ReadFieldHeader(session);
            return this.CodecProvider.GetCodec<T>().ReadValue(ref reader, session, field);
        }
    }
}