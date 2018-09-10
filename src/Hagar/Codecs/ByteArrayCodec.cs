using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class ByteArrayCodec : TypedCodecBase<byte[], ByteArrayCodec>, IFieldCodec<byte[]>
    {
        byte[] IFieldCodec<byte[]>.ReadValue(ref Reader reader, Field field)
        {
            return ReadValue(ref reader, field);
        }

        public static byte[] ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<byte[]>(ref reader, field);
            if (field.WireType != WireType.LengthPrefixed) ThrowUnsupportedWireTypeException(field);
            var length = reader.ReadVarUInt32();
            var result = reader.ReadBytes(length);
            ReferenceCodec.RecordObject(reader.Session, result);
            return result;
        }

        void IFieldCodec<byte[]>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, byte[] value)
        {
            WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, byte[] value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) return;

            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(byte[]), WireType.LengthPrefixed);
            writer.WriteVarInt((uint) value.Length);
            writer.Write(value);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for byte[] fields. {field}");
    }
}