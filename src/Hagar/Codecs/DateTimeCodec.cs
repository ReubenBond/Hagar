using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class DateTimeCodec : IFieldCodec<DateTime>
    {
        void IFieldCodec<DateTime>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, DateTime value)
        {
            WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, DateTime value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(DateTime), WireType.Fixed64);
            writer.Write(value.ToBinary());
        }

        DateTime IFieldCodec<DateTime>.ReadValue(ref Reader reader, Field field)
        {
            return ReadValue(ref reader, field);
        }

        public static DateTime ReadValue(ref Reader reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            if (field.WireType != WireType.Fixed64) ThrowUnsupportedWireTypeException(field);
            return DateTime.FromBinary(reader.ReadInt64());
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.Fixed64} is supported for {nameof(DateTime)} fields. {field}");
    }
}