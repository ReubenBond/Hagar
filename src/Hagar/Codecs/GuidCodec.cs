using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class GuidCodec : IFieldCodec<Guid>
    {
        private const int Width = 16;

        void IFieldCodec<Guid>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Guid value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Guid value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(Guid), WireType.Fixed128);
#if NETCOREAPP
            writer.EnsureContiguous(Width);
            if (value.TryWriteBytes(writer.WritableSpan))
            {
                writer.AdvanceSpan(Width);
                return;
            }
#endif
            writer.Write(value.ToByteArray());
        }

        Guid IFieldCodec<Guid>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static Guid ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
#if NETCOREAPP
            if (reader.TryReadBytes(Width, out var readOnly))
            {
                return new Guid(readOnly);
            }

            Span<byte> bytes = stackalloc byte[Width];
            for (var i = 0; i < Width; i++)
            {
                bytes[i] = reader.ReadByte();
            }

            return new Guid(bytes);
#else
            return new Guid(reader.ReadBytes(Width));
#endif
        }
    }
}