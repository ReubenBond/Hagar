using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{

    public static class ConsumeFieldExtension
    {
        /// <summary>
        /// Consumes an unknown field.
        /// </summary>
        public static void ConsumeUnknownField(this ref Reader reader, SerializerSession session, Field field)
        {
            // References cannot themselves be referenced.
            if (field.WireType == WireType.Reference)
            {
                reader.ReadVarUInt32();
                return;
            }

            // Record a placeholder so that this field can later be correctly deserialized if it is referenced.
            ReferenceCodec.RecordObject(session, new UnknownFieldMarker(field, reader.Position));
            
            switch (field.WireType)
            {
                case WireType.VarInt:
                    reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    // Since tag delimited fields can be comprised of other fields, recursively consume those, too.
                    ConsumeTagDelimitedField(ref reader, session);
                    break;
                case WireType.LengthPrefixed:
                    SkipFieldExtension.SkipLengthPrefixedField(ref reader);
                    break;
                case WireType.Fixed32:
                    reader.Skip(4);
                    break;
                case WireType.Fixed64:
                    reader.Skip(8);
                    break;
                case WireType.Fixed128:
                    reader.Skip(16);
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
        private static void ConsumeTagDelimitedField(ref Reader reader, SerializerSession session)
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