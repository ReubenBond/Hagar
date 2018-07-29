using System;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class TypeSerializerCodec : IFieldCodec<Type>
    {
        private static readonly Type SchemaTypeType = typeof(SchemaType);
        private static readonly Type TypeType = typeof(Type);
        private static readonly Type ByteArrayType = typeof(byte[]);
        private static readonly Type UIntType = typeof(uint);

        void IFieldCodec<Type>.WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Type value)
        {
            WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }

        public static void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Type value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, TypeType, WireType.TagDelimited);
            var (schemaType, id) = GetSchemaType(session, value);

            // Write the encoding type.
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, 0, SchemaTypeType, SchemaTypeType, WireType.VarInt);
            writer.WriteVarInt((uint) schemaType);

            if (schemaType == SchemaType.Encoded)
            {
                // If the type is encoded, write the length-prefixed bytes.
                ReferenceCodec.MarkValueField(session);
                writer.WriteFieldHeader(session, 1, ByteArrayType, ByteArrayType, WireType.LengthPrefixed);
                session.TypeCodec.Write(ref writer, value);
            }
            else
            {
                // If the type is referenced or well-known, write it as a varint.
                ReferenceCodec.MarkValueField(session);
                writer.WriteFieldHeader(session, 2, UIntType, UIntType, WireType.VarInt);
                writer.WriteVarInt((uint) id);
            }

            writer.WriteEndObject();
        }

        Type IFieldCodec<Type>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            return ReadValue(ref reader, session, field);
        }

        public static Type ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<Type>(ref reader, session, field);

            uint fieldId = 0;
            var schemaType = default(SchemaType);
            uint id = 0;
            Type result = null;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                ReferenceCodec.MarkValueField(session);
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        schemaType = (SchemaType) reader.ReadVarUInt32();
                        break;
                    case 1:
                        result = session.TypeCodec.Read(ref reader);
                        break;
                    case 2:
                        id = reader.ReadVarUInt32();
                        break;
                }
            }

            switch (schemaType)
            {
                case SchemaType.Referenced:
                    if (session.ReferencedTypes.TryGetReferencedType(id, out result)) return result;
                    return ThrowUnknownReferencedType(id);
                case SchemaType.WellKnown:
                    if (session.WellKnownTypes.TryGetWellKnownType(id, out result)) return result;
                    return ThrowUnknownWellKnownType(id);
                case SchemaType.Encoded:
                    if (result != null) return result;
                    return ThrowMissingType();
                default:
                    return ThrowInvalidSchemaType(schemaType);
            }
        }

        private static (SchemaType, uint) GetSchemaType(SerializerSession session, Type actualType)
        {
            if (session.WellKnownTypes.TryGetWellKnownTypeId(actualType, out uint typeId))
            {
                return (SchemaType.WellKnown, typeId);
            }

            if (session.ReferencedTypes.TryGetTypeReference(actualType, out uint reference))
            {
                return (SchemaType.Referenced, reference);
            }

            return (SchemaType.Encoded, 0);
        }

        private static Type ThrowInvalidSchemaType(SchemaType schemaType) => throw new NotSupportedException(
            $"SchemaType {schemaType} is not supported by {nameof(TypeSerializerCodec)}.");

        private static Type ThrowUnknownReferencedType(uint id) => throw new UnknownReferencedTypeException(id);
        private static Type ThrowUnknownWellKnownType(uint id) => throw new UnknownWellKnownTypeException(id);
        private static Type ThrowMissingType() => throw new TypeMissingException();
    }
}