using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Security;

namespace Hagar.ISerializable
{
    internal sealed class SerializationEntryCodec : IFieldCodec<SerializationEntrySurrogate>
    {
        public static readonly Type SerializationEntryType = typeof(SerializationEntrySurrogate);

        [SecurityCritical]
        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            SerializationEntrySurrogate value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, SerializationEntryType, WireType.TagDelimited);
            StringCodec.WriteField(ref writer, 0, typeof(string), value.Name);
            ObjectCodec.WriteField(ref writer, 1, typeof(object), value.Value);

            writer.WriteEndObject();
        }

        [SecurityCritical]
        public SerializationEntrySurrogate ReadValue(ref Reader reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            var result = new SerializationEntrySurrogate();
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        result.Name = StringCodec.ReadValue(ref reader, header);
                        break;
                    case 1:
                        result.Value = ObjectCodec.ReadValue(ref reader, header);
                        break;
                }
            }

            return result;
        }
    }
}