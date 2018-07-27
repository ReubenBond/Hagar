using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class GuidCodec : IFieldCodec<Guid>
    {
        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Guid value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(Guid), WireType.Fixed128);
            writer.Write(value);
        }

        public Guid ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            var bytes = new byte[16];
            reader.ReadSpan(bytes);
            return new Guid(bytes);
        }
    }
}