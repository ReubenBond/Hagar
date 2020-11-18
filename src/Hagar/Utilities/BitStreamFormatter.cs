using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;
using System.Text;

namespace Hagar.Utilities
{
    public static class BitStreamFormatter
    {
        public static string Format<TInput>(ref Reader<TInput> reader)
        {
            var res = new StringBuilder();
            Format(ref reader, res);
            return res.ToString();
        }

        public static void Format<TInput>(ref Reader<TInput> reader, StringBuilder result)
        {
            var (field, type) = reader.ReadFieldHeaderForAnalysis();
            FormatField(ref reader, field, type, field.FieldIdDelta, result, indentation: 0);
        }

        private static void FormatField<TInput>(ref Reader<TInput> reader, Field field, string typeName, uint id, StringBuilder res, int indentation)
        {
            var indentString = new string(' ', indentation);
            res.Append(indentString);

            // References cannot themselves be referenced.
            if (field.WireType == WireType.Reference)
            {
                ReferenceCodec.MarkValueField(reader.Session);
                var refId = reader.ReadVarUInt32();
                var exists = reader.Session.ReferencedObjects.TryGetReferencedObject(refId, out _);
                res.Append('[');
                FormatFieldHeader(res, reader.Session, field, id, typeName);
                res.Append($" Reference: {refId} ({(exists ? "exists" : "unknown")})");
                res.Append(']');
                return;
            }

            // Record a placeholder so that this field can later be correctly deserialized if it is referenced.
            ReferenceCodec.RecordObject(reader.Session, new UnknownFieldMarker(field, reader.Position));
            res.Append('[');
            FormatFieldHeader(res, reader.Session, field, id, typeName);
            res.Append(']');
            res.Append(" Value: ");

            switch (field.WireType)
            {
                case WireType.VarInt:
                    {
                        var a = reader.ReadVarUInt64();
                        if (a < 10240)
                        {
                            res.Append($"{a} (0x{a:X})");
                        }
                        else
                        {
                            res.Append($"0x{a:X}");
                        }
                    }
                    break;
                case WireType.TagDelimited:
                    // Since tag delimited fields can be comprised of other fields, recursively consume those, too.

                    res.Append($"{{\n");
                    reader.FormatTagDelimitedField(res, indentation + 1);
                    res.Append($"\n{indentString}}}");
                    break;
                case WireType.LengthPrefixed:
                    {
                        var length = reader.ReadVarUInt32();
                        res.Append($"(length: {length}b) [");
                        var a = reader.ReadBytes(length);
                        FormatByteArray(res, a);
                        res.Append(']');
                    }
                    break;
                case WireType.Fixed32:
                    {
                        var a = reader.ReadUInt32();
                        if (a < 10240)
                        {
                            res.Append($"{a} (0x{a:X})");
                        }
                        else
                        {
                            res.Append($"0x{a:X}");
                        }
                    }
                    break;
                case WireType.Fixed64:
                    {
                        var a = reader.ReadUInt64();
                        if (a < 10240)
                        {
                            res.Append($"{a} (0x{a:X})");
                        }
                        else
                        {
                            res.Append($"0x{a:X}");
                        }
                    }
                    break;
                case WireType.Fixed128:
                    {
                        var a = reader.ReadUInt64();
                        var b = reader.ReadUInt64();
                        res.Append($"{a:X16}{b:X16}");
                    }
                    break;
                case WireType.Extended:
                    SkipFieldExtension.ThrowUnexpectedExtendedWireType(field);
                    break;
                default:
                    SkipFieldExtension.ThrowUnexpectedWireType(field);
                    break;
            }
        }

        private static void FormatByteArray(StringBuilder res, byte[] a)
        {
            var isAscii = true;
            foreach (var b in a)
            {
                if (b >= 0x7F)
                {
                    isAscii = false;
                }
            }

            if (isAscii)
            {
                res.Append('"');
                res.Append(Encoding.ASCII.GetString(a));
                res.Append('"');
            }
            else
            {
                bool first = true;
                foreach (var b in a)
                {
                    if (!first)
                    {
                        res.Append(' ');
                    }
                    else
                    {
                        first = false;
                    }

                    res.Append($"{b:X2}");
                }
            }
        }

        private static void FormatFieldHeader(StringBuilder res, SerializerSession session, Field field, uint id, string typeName)
        {
            _ = res
                .Append($"#{session.ReferencedObjects.CurrentReferenceId} ")
                .Append((string)field.WireType.ToString());
            if (field.HasFieldId)
            {
                _ = res.Append($" Id: {id}");
            }

            if (field.IsSchemaTypeValid)
            {
                _ = res.Append($" SchemaType: {field.SchemaType}");
            }

            if (field.HasExtendedSchemaType)
            {
                _ = res.Append($" RuntimeType: {field.FieldType} (name: {typeName})");
            }

            if (field.WireType == WireType.Extended)
            {
                _ = res.Append($": {field.ExtendedWireType}");
            }
        }

        /// <summary>
        /// Consumes a tag-delimited field.
        /// </summary>
        private static void FormatTagDelimitedField<TInput>(this ref Reader<TInput> reader, StringBuilder res, int indentation)
        {
            var id = 0U;
            var first = true;
            while (true)
            {
                var (field, type) = reader.ReadFieldHeaderForAnalysis();
                if (field.IsEndObject)
                {
                    break;
                }

                if (field.IsEndBaseFields)
                {
                    res.Append(" | ");
                    id = 0U;
                    continue;
                }

                id += field.FieldIdDelta;
                if (!first)
                {
                    res.AppendLine();
                }
                else
                {
                    first = false;
                }

                FormatField(ref reader, field, type, id, res, indentation);
            }
        }
    }
}

