using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ObjectCodec : IFieldCodec<object>
    {
        private static readonly Type ObjectType = typeof(object);

        object IFieldCodec<object>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
            }

            if (field.FieldType == ObjectType || field.FieldType is null)
            {
                _ = reader.ReadVarUInt32();
                var result = new object();
                ReferenceCodec.RecordObject(reader.Session, result);
                return result;
            }

            var specificSerializer = reader.Session.CodecProvider.GetCodec(field.FieldType);
            return specificSerializer.ReadValue(ref reader, field);
        }

        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            var fieldType = value?.GetType();
            if (fieldType is null || fieldType == ObjectType)
            {
                if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
                {
                    return;
                }

                writer.WriteFieldHeader(fieldIdDelta, expectedType, ObjectType, WireType.LengthPrefixed);
                writer.WriteVarInt(0U);
            }

            var specificSerializer = writer.Session.CodecProvider.GetCodec(fieldType);
            specificSerializer.WriteField(ref writer, fieldIdDelta, expectedType, value);
        }
    }
}