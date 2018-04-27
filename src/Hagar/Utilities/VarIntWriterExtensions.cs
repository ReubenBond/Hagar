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
            var scratch = new byte[2];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch, 0, count);
        }

        public static void WriteVarInt(this Writer writer, ushort value)
        {
            var scratch = new byte[3];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch, 0, count);
        }

        public static void WriteVarInt(this Writer writer, uint value)
        {
            var scratch = new byte[5];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch, 0, count);
        }

        public static void WriteVarInt(this Writer writer, ulong value)
        {
            var scratch = new byte[10];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(scratch, 0, count);
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