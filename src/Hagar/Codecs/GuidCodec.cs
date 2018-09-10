using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class GuidCodec : IFieldCodec<Guid>
    {
        private const int Width = 16;

        void IFieldCodec<Guid>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Guid value)
        {
            WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Guid value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(Guid), WireType.Fixed128);
#if NETCOREAPP2_1
            writer.EnsureContiguous(Width);
            if (value.TryWriteBytes(writer.WritableSpan))
            {
                writer.AdvanceSpan(Width);
                return;
            }
#endif
            writer.Write(value.ToByteArray());
        }

        Guid IFieldCodec<Guid>.ReadValue(ref Reader reader, Field field)
        {
            return ReadValue(ref reader, field);
        }

        public static Guid ReadValue(ref Reader reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
#if NETCOREAPP2_1
            if (reader.TryReadBytes(Width, out var readOnly))
            {
                return new Guid(readOnly);
            }

            // TODO: stackalloc
            Span<byte> bytes = new byte[Width];
            reader.ReadBytes(in bytes);
            return new Guid(bytes);
#else
            return new Guid(reader.ReadBytes(Width));
#endif
        }
    }
}