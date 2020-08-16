using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for operating with the wire format.
    /// </summary>
    public static class FieldHeaderCodec
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeader<TBufferWriter>(
            ref this Writer<TBufferWriter> writer,
            uint fieldId,
            Type expectedType,
            Type actualType,
            WireType wireType) where TBufferWriter : IBufferWriter<byte>
        {
            var hasExtendedFieldId = fieldId > Tag.MaxEmbeddedFieldIdDelta;
            var embeddedFieldId = hasExtendedFieldId ? Tag.FieldIdCompleteMask : (byte)fieldId;
            var tag = (byte)((byte)wireType | embeddedFieldId);

            if (actualType == expectedType)
            {
                writer.Write((byte)(tag | (byte)SchemaType.Expected));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
            }
            else if (writer.Session.WellKnownTypes.TryGetWellKnownTypeId(actualType, out var typeOrReferenceId))
            {
                writer.Write((byte)(tag | (byte)SchemaType.WellKnown));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                writer.WriteVarInt(typeOrReferenceId);
            }
            else if (writer.Session.ReferencedTypes.TryGetTypeReference(actualType, out typeOrReferenceId))
            {
                writer.Write((byte)(tag | (byte)SchemaType.Referenced));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                writer.WriteVarInt(typeOrReferenceId);
            }
            else
            {
                writer.Write((byte)(tag | (byte)SchemaType.Encoded));
                if (hasExtendedFieldId) writer.WriteVarInt(fieldId);
                writer.Session.TypeCodec.Write(ref writer, actualType);
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
            writer.Write((byte)((byte)wireType | (byte)fieldId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFieldHeaderExpectedExtended<TBufferWriter>(this ref Writer<TBufferWriter> writer, uint fieldId, WireType wireType)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.Write((byte)((byte)wireType | Tag.FieldIdCompleteMask));
            writer.WriteVarInt(fieldId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFieldHeader(ref this Reader reader, ref Field field)
        {
            var tag = reader.ReadByte();

            if (tag != (byte)WireType.Extended && ((tag & Tag.FieldIdCompleteMask) == Tag.FieldIdCompleteMask || (tag & Tag.SchemaTypeMask) != (byte)SchemaType.Expected))
            {
                field.Tag = tag;
                ReadFieldHeaderSlow(ref reader, ref field);
            }
            else
            {
                field.Tag = tag;
                field.FieldIdDeltaRaw = default;
                field.FieldTypeRaw = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Field ReadFieldHeader(ref this Reader reader)
        {
            Field field = default;
            var tag = reader.ReadByte();
            if (tag != (byte)WireType.Extended && ((tag & Tag.FieldIdCompleteMask) == Tag.FieldIdCompleteMask || (tag & Tag.SchemaTypeMask) != (byte)SchemaType.Expected))
            {
                field.Tag = tag;
                ReadFieldHeaderSlow(ref reader, ref field);
            }
            else
            {
                field.Tag = tag;
                field.FieldIdDeltaRaw = default;
                field.FieldTypeRaw = default;
            }

            return field;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReadFieldHeaderSlow(ref this Reader reader, ref Field field)
        {
            // If all of the field id delta bits are set and the field isn't an extended wiretype field, read the extended field id delta
            var notExtended = (field.Tag & (byte)WireType.Extended) != (byte)WireType.Extended;
            if ((field.Tag & Tag.FieldIdCompleteMask) == Tag.FieldIdCompleteMask && notExtended)
            {
                field.FieldIdDeltaRaw = reader.ReadVarUInt32NoInlining();
            }
            else
            {
                field.FieldIdDeltaRaw = 0;
            }

            // If schema type is valid, read the type.
            var schemaType = (SchemaType)(field.Tag & Tag.SchemaTypeMask);
            if (notExtended && schemaType != SchemaType.Expected)
            {
                field.FieldTypeRaw = reader.ReadType(schemaType);
            }
            else
            {
                field.FieldTypeRaw = default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Type ReadType(this ref Reader reader, SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    return null;
                case SchemaType.WellKnown:
                    var typeId = reader.ReadVarUInt32();
                    return reader.Session.WellKnownTypes.GetWellKnownType(typeId);
                case SchemaType.Encoded:
                    reader.Session.TypeCodec.TryRead(ref reader, out Type encoded);
                    return encoded;
                case SchemaType.Referenced:
                    var reference = reader.ReadVarUInt32();
                    return reader.Session.ReferencedTypes.GetReferencedType(reference);
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<Type>(nameof(SchemaType));
            }
        }
    }
}