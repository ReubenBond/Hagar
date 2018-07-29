using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class GuidCodec : IFieldCodec<Guid>
    {
        private const int Width = 16;

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Guid value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(Guid), WireType.Fixed128);
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

        public Guid ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
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