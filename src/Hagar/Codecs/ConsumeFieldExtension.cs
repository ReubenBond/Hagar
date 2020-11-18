using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public static class ConsumeFieldExtension
    {
        /// <summary>
        /// Consumes an unknown field.
        /// </summary>
        public static void ConsumeUnknownField<TInput>(this ref Reader<TInput> reader, Field field)
        {
            // References cannot themselves be referenced.
            if (field.WireType == WireType.Reference)
            {
                ReferenceCodec.MarkValueField(reader.Session);
                _ = reader.ReadVarUInt32();
                return;
            }

            // Record a placeholder so that this field can later be correctly deserialized if it is referenced.
            ReferenceCodec.RecordObject(reader.Session, new UnknownFieldMarker(field, reader.Position));

            switch (field.WireType)
            {
                case WireType.VarInt:
                    _ = reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    // Since tag delimited fields can be comprised of other fields, recursively consume those, too.
                    reader.ConsumeTagDelimitedField();
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
        private static void ConsumeTagDelimitedField<TInput>(this ref Reader<TInput> reader)
        {
            while (true)
            {
                var field = reader.ReadFieldHeader();
                if (field.IsEndObject)
                {
                    break;
                }

                if (field.IsEndBaseFields)
                {
                    continue;
                }

                reader.ConsumeUnknownField(field);
            }
        }
    }
}