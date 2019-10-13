using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class ObjectCodec : IFieldCodec<object>
    {
        private static readonly Type ObjectType = typeof(object);

        object IFieldCodec<object>.ReadValue(ref Reader reader, Field field)
        {
            return ReadValue(ref reader, field);
        }

        public static object ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<object>(ref reader, field);
            if (field.FieldType == ObjectType || field.FieldType is null)
            {
                reader.ReadVarUInt32();
                var result = new object();
                ReferenceCodec.RecordObject(reader.Session, result);
                return result;
            }

            var specificSerializer = reader.Session.CodecProvider.GetCodec(field.FieldType);
            return specificSerializer.ReadValue(ref reader, field);
        }

        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        {
            WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            var fieldType = value?.GetType();
            if (fieldType is null || fieldType == ObjectType)
            {
                if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) return;
                writer.WriteFieldHeader(fieldIdDelta, expectedType, ObjectType, WireType.LengthPrefixed);
                writer.WriteVarInt((uint) 0);
            }

            var specificSerializer = writer.Session.CodecProvider.GetCodec(fieldType);
            specificSerializer.WriteField(ref writer, fieldIdDelta, expectedType, value);
        }
    }
}