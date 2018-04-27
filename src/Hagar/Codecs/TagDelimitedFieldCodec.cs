using System;
using System.Runtime.CompilerServices;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public static class TagDelimitedFieldCodec
    {
        private static readonly byte EndObjectTag = new Tag
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.EndTagDelimited
        };

        private static readonly byte EndBaseFieldsTag = new Tag
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.EndBaseFields
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStartObject(
            this Writer writer,
            SerializerSession session,
            uint fieldId,
            Type expectedType,
            Type actualType)
        {
            writer.WriteFieldHeader(session, fieldId, expectedType, actualType, WireType.TagDelimited);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteEndObject(this Writer writer)
        {
            writer.Write((byte) EndObjectTag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteEndBase(this Writer writer)
        {
            writer.Write((byte) EndBaseFieldsTag);
        }
    }
}