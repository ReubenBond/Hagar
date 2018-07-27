using System;
using Hagar.Buffers;

namespace Hagar.Utilities
{
    public static class VarIntWriterExtensions
    {
        public static void WriteVarInt(this Writer writer, sbyte value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, short value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, int value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, long value) => WriteVarInt(writer, ZigZagEncode(value));

        public static void WriteVarInt(this Writer writer, byte value)
        {
            Span<byte> scratch = stackalloc byte[2];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch.Slice(0, count));
        }

        public static void WriteVarInt(this Writer writer, ushort value)
        {
            Span<byte> scratch = stackalloc byte[3];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch.Slice(0, count));
        }

        public static void WriteVarInt(this Writer writer, uint value)
        {
            Span<byte> scratch = stackalloc byte[5];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch.Slice(0, count));
        }

        public static void WriteVarInt(this Writer writer, ulong value)
        {
            Span<byte> scratch = stackalloc byte[10];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch.Slice(0, count));
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