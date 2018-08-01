using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class ObjectCodec : IFieldCodec<object>
    {
        private static readonly Type ObjectType = typeof(object);

        object IFieldCodec<object>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            return ReadValue(ref reader, session, field);
        }

        public static object ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<object>(ref reader, session, field);
            if (field.FieldType == ObjectType || field.FieldType == null)
            {
                reader.ReadVarUInt32();
                return new object();
            }

            var specificSerializer = session.CodecProvider.GetCodec(field.FieldType);
            return specificSerializer.ReadValue(ref reader, session, field);
        }

        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            var fieldType = value?.GetType();
            if (fieldType == null || fieldType == ObjectType)
            {
                if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, ObjectType, WireType.LengthPrefixed);
                writer.WriteVarInt((uint) 0);
            }

            var specificSerializer = session.CodecProvider.GetCodec(fieldType);
            specificSerializer.WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }
    }
}