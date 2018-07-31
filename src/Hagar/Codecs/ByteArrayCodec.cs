using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class ByteArrayCodec : TypedCodecBase<byte[], ByteArrayCodec>, IFieldCodec<byte[]>
    {
        byte[] IFieldCodec<byte[]>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            return ReadValue(ref reader, session, field);
        }

        public static byte[] ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<byte[]>(ref reader, session, field);
            if (field.WireType != WireType.LengthPrefixed) ThrowUnsupportedWireTypeException(field);
            var length = reader.ReadVarUInt32();
            var result = reader.ReadBytes(length);
            ReferenceCodec.RecordObject(session, result);
            return result;
        }

        void IFieldCodec<byte[]>.WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, byte[] value)
        {
            WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }

        public static void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, byte[] value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;

            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(byte[]), WireType.LengthPrefixed);
            writer.WriteVarInt((uint) value.Length);
            writer.Write(value);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for byte[] fields. {field}");
    }
}