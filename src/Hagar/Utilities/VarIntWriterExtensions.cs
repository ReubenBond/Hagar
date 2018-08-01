using System.Buffers;
using System.Runtime.CompilerServices;
using Hagar.Buffers;

namespace Hagar.Utilities
{
    public static class VarIntWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, sbyte value) where TBufferWriter : IBufferWriter<byte> => WriteVarInt(ref writer, ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, short value) where TBufferWriter : IBufferWriter<byte> => WriteVarInt(ref writer, ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, int value) where TBufferWriter : IBufferWriter<byte> => writer.WriteVarInt(ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, long value) where TBufferWriter : IBufferWriter<byte> => WriteVarInt(ref writer, ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, byte value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.EnsureContiguous(2);
            var count = 0;
            var span = writer.WritableSpan;
            do
            {
                span[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            span[count - 1] &= 0x7F; // adjust the last byte.
            writer.AdvanceSpan(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, ushort value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.EnsureContiguous(3);

            var count = 0;
            var span = writer.WritableSpan;
            do
            {
                span[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            span[count - 1] &= 0x7F; // adjust the last byte.
            writer.AdvanceSpan(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, ulong value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.EnsureContiguous(10);
            var count = 0;
            var span = writer.WritableSpan;
            do
            {
                span[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            span[count - 1] &= 0x7F; // adjust the last byte.
            writer.AdvanceSpan(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ZigZagEncode(sbyte value)
        {
            return (byte)((value << 1) ^ (value >> 7));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ZigZagEncode(short value)
        {
            return (ushort)((value << 1) ^ (value >> 15));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ZigZagEncode(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ZigZagEncode(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
    }
}