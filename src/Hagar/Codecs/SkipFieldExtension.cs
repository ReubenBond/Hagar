using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class SkipFieldCodec : IFieldCodec<object>
    {
        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            ReferenceCodec.MarkValueField(session);
            throw new NotImplementedException();
        }

        public object ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            reader.SkipField(session, field);
            return null;
        }
    }

    public static class SkipFieldExtension
    {
        public static void SkipField(this ref Reader reader, SerializerSession session, Field field)
        {
            switch (field.WireType)
            {
                case WireType.Reference:
                case WireType.VarInt:
                    reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    SkipTagDelimitedField(ref reader, session);
                    break;
                case WireType.LengthPrefixed:
                    SkipLengthPrefixedField(ref reader);
                    break;
                case WireType.Fixed32:
                    reader.ReadUInt32();
                    break;
                case WireType.Fixed64:
                    reader.ReadUInt64();
                    break;
                case WireType.Fixed128:
                    reader.ReadUInt64();
                    reader.ReadUInt64();
                    break;
                case WireType.Extended:
                    if (!field.IsEndBaseOrEndObject)
                        ThrowUnexpectedExtendedWireType(field);
                    break;
                default:
                    ThrowUnexpectedWireType(field);
                    break;
            }
        }

        internal static void ThrowUnexpectedExtendedWireType(Field field)
        {
            throw new ArgumentOutOfRangeException(
                $"Unexpected {nameof(ExtendedWireType)} value [{field.ExtendedWireType}] in field {field} while skipping field.");
        }

        internal static void ThrowUnexpectedWireType(Field field)
        {
            throw new ArgumentOutOfRangeException(
                $"Unexpected {nameof(WireType)} value [{field.WireType}] in field {field} while skipping field.");
        }

        internal static void SkipLengthPrefixedField(ref Reader reader)
        {
            var length = reader.ReadVarUInt32();
            reader.Skip(length);
        }

        private static void SkipTagDelimitedField(ref Reader reader, SerializerSession session)
        {
            while (true)
            {
                var field = reader.ReadFieldHeader(session);
                if (field.IsEndObject) break;
                reader.SkipField(session, field);
            }
        }
    }
}