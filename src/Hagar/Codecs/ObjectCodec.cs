using System;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class ObjectCodec : IFieldCodec<object>
    {
        private readonly IUntypedCodecProvider codecProvider;
        private static readonly Type ObjectType = typeof(object);

        public ObjectCodec(IUntypedCodecProvider codecProvider)
        {
            this.codecProvider = codecProvider;
        }

        public object ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<object>(ref reader, session, field, this.codecProvider);
            if (field.FieldType == ObjectType || field.FieldType == null)
            {
                reader.ReadVarUInt32();
                return new object();
            }

            var specificSerializer = this.codecProvider.GetCodec(field.FieldType);
            return specificSerializer.ReadValue(ref reader, session, field);
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            var fieldType = value?.GetType();
            if (fieldType == null || fieldType == ObjectType)
            {
                if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, ObjectType, WireType.LengthPrefixed);
                writer.WriteVarInt((uint) 0);
            }

            var specificSerializer = this.codecProvider.GetCodec(fieldType);
            specificSerializer.WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }
    }
}