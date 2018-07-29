using System;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    internal class SerializationEntryCodec : IFieldCodec<SerializationEntrySurrogate>
    {
        private readonly IFieldCodec<string> stringCodec;
        private readonly IFieldCodec<object> objectCodec;
        public static readonly Type SerializationEntryType = typeof(SerializationEntrySurrogate);

        public SerializationEntryCodec(IFieldCodec<string> stringCodec, IFieldCodec<object> objectCodec)
        {
            this.stringCodec = stringCodec;
            this.objectCodec = objectCodec;
        }

        public void WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            SerializationEntrySurrogate value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, SerializationEntryType, WireType.TagDelimited);
            this.stringCodec.WriteField(ref writer, session, 0, typeof(string), value.Name);
            this.objectCodec.WriteField(ref writer, session, 1, typeof(object), value.Value);
            
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
                        result.Name = this.stringCodec.ReadValue(ref reader, session, header);
                        break;
                    case 1:
                        result.Value = this.objectCodec.ReadValue(ref reader, session, header);
                        break;
                }
            }

            return result;
        }
    }
}