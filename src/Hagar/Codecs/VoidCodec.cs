using Hagar.Buffers;
using Hagar.WireProtocol;
using System;

namespace Hagar.Codecs
{
    public sealed class VoidCodec : IFieldCodec<object>
    {
        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        {
            if (!ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                ThrowNotNullException(value);
            }
        }

        object IFieldCodec<object>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.Reference)
            {
                ThrowInvalidWireType(field);
            }

            return ReferenceCodec.ReadReference<object>(ref reader, field);
        }

        private static void ThrowInvalidWireType(Field field) => throw new UnsupportedWireTypeException($"Expected a reference, but encountered wire type of '{field.WireType}'.");

        private static void ThrowNotNullException(object value) => throw new InvalidOperationException(
            $"Expected a value of null, but encountered a value of '{value}'.");
    }
}