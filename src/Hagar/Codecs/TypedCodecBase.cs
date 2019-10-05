using System;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class TypedCodecBase<TField, TCodec> : IFieldCodec<object> where TCodec : class, IFieldCodec<TField>
    {
        private readonly TCodec codec;

        public TypedCodecBase()
        {
            this.codec = this as TCodec;
            if (this.codec is null) ThrowInvalidSubclass();
        }

        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        {
            this.codec.WriteField(ref writer, fieldIdDelta, expectedType, (TField)value);
        }

        object IFieldCodec<object>.ReadValue(ref Reader reader, Field field)
        {
            return this.codec.ReadValue(ref reader, field);
        }

        private static void ThrowInvalidSubclass()
        {
            throw new InvalidCastException($"Subclasses of {typeof(TypedCodecBase<TField, TCodec>)} must implement/derive from {typeof(TCodec)}.");
        }
    }
}