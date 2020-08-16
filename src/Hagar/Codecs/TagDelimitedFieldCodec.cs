using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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
        public static void WriteStartObject<TBufferWriter>(
            ref this Writer<TBufferWriter> writer,
            uint fieldId,
            Type expectedType,
            Type actualType) where TBufferWriter : IBufferWriter<byte> => writer.WriteFieldHeader(fieldId, expectedType, actualType, WireType.TagDelimited);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteEndObject<TBufferWriter>(this ref Writer<TBufferWriter> writer) where TBufferWriter : IBufferWriter<byte> => writer.Write((byte)EndObjectTag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteEndBase<TBufferWriter>(this ref Writer<TBufferWriter> writer) where TBufferWriter : IBufferWriter<byte> => writer.Write((byte)EndBaseFieldsTag);
    }
}