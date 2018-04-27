using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Marker object used to denote an unknown field and its offset into a stream of data.
    /// </summary>
    public class UnknownFieldMarker
    {
        public UnknownFieldMarker(Field field, int offset)
        {
            this.Field = field;
            this.Offset = offset;
        }

        /// <summary>
        /// The offset into the stream at which this field occurs.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The field header.
        /// </summary>
        public Field Field { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(UnknownFieldMarker)}] {nameof(this.Offset)}: {this.Offset}, {nameof(this.Field)}: {this.Field}";
        }
    }

    public static class ConsumeFieldExtension
    {
/*        public static Reader GetReaderForReference(this Reader reader, SerializerSession session, uint reference)
        {
            if (!session.ReferencedObjects.TryGetReferencedObject(reference, out object referencedObject)) ThrowReferenceNotFound();
            if (referencedObject is UnknownFieldMarker marker)
            {
                var result = new Reader(reader.GetBuffers());
                result.Advance(marker.Offset);
                return result;
            }
        }*/

        /// <summary>
        /// Consumes an unknown field.
        /// </summary>
        public static void ConsumeUnknownField(this Reader reader, SerializerSession session, Field field)
        {
            // References cannot themselves be referenced.
            if (field.WireType == WireType.Reference)
            {
                reader.ReadVarUInt32();
                return;
            }

            // Record a placeholder so that this field can later be correctly deserialized if it is referenced.
            ReferenceCodec.RecordObject(session, new UnknownFieldMarker(field, reader.CurrentPosition));

            // TODO: Advance the reader without actually reading bytes / allocating.
            switch (field.WireType)
            {
                case WireType.VarInt:
                    reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    // Since tag delimited fields can be comprised of other fields, recursively consume those, too.
                    ConsumeTagDelimitedField(reader, session);
                    break;
                case WireType.LengthPrefixed:
                    SkipFieldExtension.SkipLengthPrefixedField(reader);
                    break;
                case WireType.Fixed32:
                    reader.ReadUInt();
                    break;
                case WireType.Fixed64:
                    reader.ReadULong();
                    break;
                case WireType.Fixed128:
                    reader.ReadULong();
                    reader.ReadULong();
                    break;
                case WireType.Extended:
                    SkipFieldExtension.ThrowUnexpectedExtendedWireType(field);
                    break;
                default:
                    SkipFieldExtension.ThrowUnexpectedWireType(field);
                    break;
            }
        }
        
        /// <summary>
        /// Consumes a tag-delimited field.
        /// </summary>
        private static void ConsumeTagDelimitedField(Reader reader, SerializerSession session)
        {
            while (true)
            {
                var field = reader.ReadFieldHeader(session);
                if (field.IsEndObject) break;
                if (field.IsEndBaseFields) continue;
                reader.ConsumeUnknownField(session, field);
            }
        }
    }
}