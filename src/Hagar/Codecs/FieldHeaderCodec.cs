using System;
using System.Buffers;
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
        public static void WriteFieldHeader<TBufferWriter>(this ref Writer<TBufferWriter> writer,
            SerializerSession session,
            uint fieldId,
            Type expectedType,
            Type actualType,
            WireType wireType) where TBufferWriter : IBufferWriter<byte>
        {
            var hasExtendedFieldId = fieldId > Tag.MaxEmbeddedFieldIdDelta;
            var embeddedFieldId = hasExtendedFieldId ? Tag.FieldIdCompleteMask : (byte) fieldId;
            var tag = (byte) ((byte) wireType | embeddedFieldId);

            if (actualType == expectedType)
            {
                writer.Write((byte) (tag | (byte) SchemaType.Expected));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
            }
            else if (session.WellKnownTypes.TryGetWellKnownTypeId(actualType, out var typeOrReferenceId))
            {
                writer.Write((byte) (tag | (byte) SchemaType.WellKnown));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                writer.WriteVarInt(typeOrReferenceId);
            }
            else if (session.ReferencedTypes.TryGetTypeReference(actualType, out typeOrReferenceId))
            {
                writer.Write((byte) (tag | (byte) SchemaType.Referenced));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                writer.WriteVarInt(typeOrReferenceId);
            }
            else
            {
                writer.Write((byte) (tag | (byte) SchemaType.Encoded));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                session.TypeCodec.Write(ref writer, actualType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeaderExpected<TBufferWriter>(this ref Writer<TBufferWriter> writer, uint fieldId, WireType wireType)
            where TBufferWriter : IBufferWriter<byte>
        {
            if (fieldId < Tag.MaxEmbeddedFieldIdDelta) WriteFieldHeaderExpectedEmbedded(ref writer, fieldId, wireType);
            else WriteFieldHeaderExpectedExtended(ref writer, fieldId, wireType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeaderExpectedEmbedded<TBufferWriter>(this ref Writer<TBufferWriter> writer, uint fieldId, WireType wireType)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.Write((byte) ((byte) wireType | (byte) fieldId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeaderExpectedExtended<TBufferWriter>(this ref Writer<TBufferWriter> writer, uint fieldId, WireType wireType)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.Write((byte) ((byte) wireType | Tag.FieldIdCompleteMask));
            writer.WriteVarInt(fieldId);
        }

        public static Field ReadFieldHeader(this ref Reader reader, SerializerSession session)
        {
            Type type = null;
            uint extendedId = 0;
            var tag = reader.ReadByte();

            // If all of the field id delta bits are set and the field isn't an extended wiretype field, read the extended field id delta
            if ((tag & Tag.FieldIdCompleteMask) == Tag.FieldIdCompleteMask && (tag & (byte) WireType.Extended) != (byte) WireType.Extended)
            {
                extendedId = reader.ReadVarUInt32();
            }

            // If schema type is valid, read the type.

            if ((tag & (byte) WireType.Extended) != (byte) WireType.Extended)
            {
                type = reader.ReadType(session, (SchemaType) (tag & Tag.SchemaTypeMask));
            }

            return new Field(tag, extendedId, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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