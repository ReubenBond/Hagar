using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class NullableCodec<T> : IFieldCodec<T?> where T : struct
    {
        private readonly IFieldCodec<T> _fieldCodec;

        public NullableCodec(IFieldCodec<T> fieldCodec)
        {
            _fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
        }

        void IFieldCodec<T?>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, T? value)
        {
            // If the value is null, write it as the null reference.
            if (!value.HasValue && ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, null))
            {
                return;
            }

            // The value is not null.
            _fieldCodec.WriteField(ref writer, fieldIdDelta, typeof(T), value.Value);
        }

        T? IFieldCodec<T?>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            // This will only be true if the value is null.
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<T?, TInput>(ref reader, field);
            }

            // Read the non-null value.
            return _fieldCodec.ReadValue(ref reader, field);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        private static void ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T?)} with declared length {length}.");

        private static void ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }
}