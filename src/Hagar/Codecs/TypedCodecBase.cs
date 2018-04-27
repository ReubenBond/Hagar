using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class TypedCodecBase<TField, TCodec> : IFieldCodec<object> where TCodec : class, IFieldCodec<TField>
    {
        private readonly TCodec codec;

        public TypedCodecBase()
        {
            this.codec = this as TCodec;
            if (this.codec == null) ThrowInvalidSubclass();
        }

        void IFieldCodec<object>.WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            this.codec.WriteField(writer, session, fieldIdDelta, expectedType, (TField)value);
        }

        object IFieldCodec<object>.ReadValue(Reader reader, SerializerSession session, Field field)
        {
            return this.codec.ReadValue(reader, session, field);
        }

        private static void ThrowInvalidSubclass()
        {
            throw new InvalidCastException($"Subclasses of {typeof(TypedCodecBase<TField, TCodec>)} must implement/derive from {typeof(TCodec)}.");
        }
    }
}