using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class TimeSpanCodec : IFieldCodec<TimeSpan>
    {
        void IFieldCodec<TimeSpan>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TimeSpan value)
        {
            WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TimeSpan value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(TimeSpan), WireType.Fixed64);
            writer.Write(value.Ticks);
        }

        TimeSpan IFieldCodec<TimeSpan>.ReadValue(ref Reader reader, Field field)
        {
            return ReadValue(ref reader, field);
        }

        public static TimeSpan ReadValue(ref Reader reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            if (field.WireType != WireType.Fixed64) ThrowUnsupportedWireTypeException(field);
            return TimeSpan.FromTicks(reader.ReadInt64());
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.Fixed64} is supported for {nameof(TimeSpan)} fields. {field}");
    }
}