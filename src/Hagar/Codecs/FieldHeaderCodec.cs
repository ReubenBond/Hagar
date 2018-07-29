using System;
using System.Runtime.CompilerServices;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for operating with the wire format.
    /// </summary>
    public static class FieldHeaderCodec
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeader(this ref Writer writer, SerializerSession session, uint fieldId, Type expectedType, Type actualType, WireType wireType)
        {
            var field = default(Field);
            field.FieldIdDelta = fieldId;
            uint idOrReference;
            (field.SchemaType, idOrReference) = GetSchemaTypeWithEncoding(session, expectedType, actualType);
            field.WireType = wireType;

            writer.Write(field.Tag);
            if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldIdDelta);
            if (field.HasExtendedSchemaType) writer.WriteType(session, field.SchemaType, idOrReference, actualType);
        }

        public static Field ReadFieldHeader(this ref Reader reader, SerializerSession session)
        {
            var field = default(Field);
            field.Tag = reader.ReadByte();
            if (field.HasExtendedFieldId) field.FieldIdDelta = reader.ReadVarUInt32();
            if (field.IsSchemaTypeValid) field.FieldType = reader.ReadType(session, field.SchemaType);

            return field;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (SchemaType, uint) GetSchemaTypeWithEncoding(SerializerSession session, Type expectedType, Type actualType)
        {
            if (actualType == expectedType)
            {
                return (SchemaType.Expected, 0);
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteType(this ref Writer writer, SerializerSession session, SchemaType schemaType, uint idOrReference, Type type)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    break;
                case SchemaType.WellKnown:
                case SchemaType.Referenced:
                    writer.WriteVarInt(idOrReference);
                    break;
                case SchemaType.Encoded:
                    session.TypeCodec.Write(ref writer, type);
                    break;
                default:
                    ExceptionHelper.ThrowArgumentOutOfRange(nameof(schemaType));
                    break;
            }
        }

        private static Type ReadType(this ref Reader reader, SerializerSession session, SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    return null;
                case SchemaType.WellKnown:
                    var typeId = reader.ReadVarUInt32();
                    return session.WellKnownTypes.GetWellKnownType(typeId);
                case SchemaType.Encoded:
                    session.TypeCodec.TryRead(ref reader, out Type encoded);
                    return encoded;
                case SchemaType.Referenced:
                    var reference = reader.ReadVarUInt32();
                    return session.ReferencedTypes.GetReferencedType(reference);
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<Type>(nameof(SchemaType));
            }
        }
    }
}
