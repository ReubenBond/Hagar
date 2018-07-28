using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class GuidCodec : IFieldCodec<Guid>
    {
        private const int Width = 16;

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Guid value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(Guid), WireType.Fixed128);
#if NETCOREAPP2_1
            if (value.TryWriteBytes(writer.GetSpan(Width)))
            {
                writer.Advance(Width);
                return;
            }
#endif
            writer.Write(value.ToByteArray());
        }

        public Guid ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);

#if NETCOREAPP2_1
            if (reader.TryReadBytes(Width, out var readOnly))
            {
                return new Guid(readOnly);
            }
            
            Span<byte> bytes = stackalloc byte[Width];
            reader.ReadBytes(in bytes);
            return new Guid(bytes);
#else
            return new Guid(reader.ReadBytes(Width));
#endif
        }
    }
}