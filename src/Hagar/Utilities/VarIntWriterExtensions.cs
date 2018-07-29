using Hagar.Buffers;

namespace Hagar.Utilities
{
    public static class VarIntWriterExtensions
    {
        public static void WriteVarInt(ref this Writer writer, sbyte value) => WriteVarInt(ref writer, ZigZagEncode(value));
        public static void WriteVarInt(ref this Writer writer, short value) => WriteVarInt(ref writer, ZigZagEncode(value));
        public static void WriteVarInt(ref this Writer writer, int value) => WriteVarInt(ref writer, ZigZagEncode(value));
        public static void WriteVarInt(ref this Writer writer, long value) => WriteVarInt(ref writer, ZigZagEncode(value));

        public static void WriteVarInt(ref this Writer writer, byte value)
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

        public static void WriteVarInt(ref this Writer writer, ushort value)
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

        public static void WriteVarInt(ref this Writer writer, uint value)
        {
            writer.EnsureContiguous(5);
            var count = 0;
            var span = writer.WritableSpan;
            do
            {
                span[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            span[count - 1] &= 0x7F; // adjust the last byte.
            writer.AdvanceSpan(count);
        }

        public static void WriteVarInt(ref this Writer writer, ulong value)
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

        private static byte ZigZagEncode(sbyte value)
        {
            return (byte)((value << 1) ^ (value >> 7));
        }

        private static ushort ZigZagEncode(short value)
        {
            return (ushort)((value << 1) ^ (value >> 15));
        }

        private static uint ZigZagEncode(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        private static ulong ZigZagEncode(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
    }
}