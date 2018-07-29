using System;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer for value types.
    /// </summary>
    /// <typeparam name="TField">The field type.</typeparam>
    public class ValueSerializer<TField> : IFieldCodec<TField> where TField : struct
    {
        private static readonly Type CodecFieldType = typeof(TField);
        private readonly IValueSerializer<TField> serializer;

        public ValueSerializer(IValueSerializerProvider provider)
        {
            this.serializer = provider.GetValueSerializer<TField>();
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteStartObject(session, fieldIdDelta, expectedType, CodecFieldType);
            this.serializer.Serialize(ref writer, session, ref value);
            writer.WriteEndObject();
        }

        public TField ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            var value = default(TField);
            ReferenceCodec.RecordObject(session, value);
            this.serializer.Deserialize(ref reader, session, ref value);
            return value;
        }
    }
}