using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    internal sealed class SerializationEntryCodec : IFieldCodec<SerializationEntrySurrogate>
    {
        public static readonly Type SerializationEntryType = typeof(SerializationEntrySurrogate);

        public void WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            SerializationEntrySurrogate value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, SerializationEntryType, WireType.TagDelimited);
            StringCodec.WriteField(ref writer, session, 0, typeof(string), value.Name);
            ObjectCodec.WriteField(ref writer, session, 1, typeof(object), value.Value);
            
            writer.WriteEndObject();
        }

        public SerializationEntrySurrogate ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            var result = new SerializationEntrySurrogate();
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        result.Name = StringCodec.ReadValue(ref reader, session, header);
                        break;
                    case 1:
                        result.Value = ObjectCodec.ReadValue(ref reader, session, header);
                        break;
                }
            }

            return result;
        }
    }
}