using System;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Methods for adapting typed and untyped codecs
    /// </summary>
    internal static class CodecAdapter
    {
        /// <summary>
        /// Converts a strongly-typed codec into an untyped codec.
        /// </summary>
        public static IFieldCodec<object> CreateUntypedFromTyped<TField, TCodec>(TCodec typedCodec) where TCodec : IFieldCodec<TField>
        {
            return new TypedCodecWrapper<TField, TCodec>(typedCodec);
        }

        /// <summary>
        /// Converts an untyped codec into a strongly-typed codec.
        /// </summary>
        public static IFieldCodec<TField> CreatedTypedFromUntyped<TField>(IFieldCodec<object> untypedCodec)
        {
            return new UntypedCodecWrapper<TField>(untypedCodec);
        }

        private class TypedCodecWrapper<TField, TCodec> : IFieldCodec<object>, IWrappedCodec where TCodec : IFieldCodec<TField>
        {
            private readonly TCodec codec;

            public TypedCodecWrapper(TCodec codec)
            {
                this.codec = codec;
            }

            public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
            {
                this.codec.WriteField(ref writer, session, fieldIdDelta, expectedType, (TField)value);
            }

            public object ReadValue(ref Reader reader, SerializerSession session, Field field)
            {
                return this.codec.ReadValue(ref reader, session, field);
            }

            public object InnerCodec => this.codec;
        }

        private class UntypedCodecWrapper<TField> : IWrappedCodec, IFieldCodec<TField>
        {
            private readonly IFieldCodec<object> codec;

            public UntypedCodecWrapper(IFieldCodec<object> codec)
            {
                this.codec = codec;
            }

            public object InnerCodec => this.codec;
            public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value)
            {
                this.codec.WriteField(ref writer, session, fieldIdDelta, expectedType, value);
            }

            public TField ReadValue(ref Reader reader, SerializerSession session, Field field)
            {
                return (TField)this.codec.ReadValue(ref reader, session, field);
            }
        }
    }
}