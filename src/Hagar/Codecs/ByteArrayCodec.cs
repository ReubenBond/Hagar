using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    public sealed class ByteArrayCodec : TypedCodecBase<byte[], ByteArrayCodec>, IFieldCodec<byte[]>
    {
        byte[] IFieldCodec<byte[]>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static byte[] ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<byte[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.LengthPrefixed)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var length = reader.ReadVarUInt32();
            var result = reader.ReadBytes(length);
            ReferenceCodec.RecordObject(reader.Session, result);
            return result;
        }

        void IFieldCodec<byte[]>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, byte[] value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, byte[] value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(byte[]), WireType.LengthPrefixed);
            writer.WriteVarInt((uint)value.Length);
            writer.Write(value);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for byte[] fields. {field}");
    }

    public sealed class ReadOnlyMemoryOfByteCodec : TypedCodecBase<ReadOnlyMemory<byte>, ReadOnlyMemoryOfByteCodec>, IFieldCodec<ReadOnlyMemory<byte>>
    {
        ReadOnlyMemory<byte> IFieldCodec<ReadOnlyMemory<byte>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static byte[] ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<byte[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.LengthPrefixed)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var length = reader.ReadVarUInt32();
            var result = reader.ReadBytes(length);
            ReferenceCodec.RecordObject(reader.Session, result);
            return result;
        }

        void IFieldCodec<ReadOnlyMemory<byte>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ReadOnlyMemory<byte> value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ReadOnlyMemory<byte> value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(byte[]), WireType.LengthPrefixed);
            writer.WriteVarInt((uint)value.Length);
            writer.Write(value.Span);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for ReadOnlyMemory<byte> fields. {field}");
    }

    public sealed class MemoryOfByteCodec : TypedCodecBase<Memory<byte>, MemoryOfByteCodec>, IFieldCodec<Memory<byte>>
    {
        Memory<byte> IFieldCodec<Memory<byte>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static byte[] ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<byte[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.LengthPrefixed)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var length = reader.ReadVarUInt32();
            var result = reader.ReadBytes(length);
            ReferenceCodec.RecordObject(reader.Session, result);
            return result;
        }

        void IFieldCodec<Memory<byte>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Memory<byte> value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Memory<byte> value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(byte[]), WireType.LengthPrefixed);
            writer.WriteVarInt((uint)value.Length);
            writer.Write(value.Span);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for ReadOnlyMemory<byte> fields. {field}");
    }
}