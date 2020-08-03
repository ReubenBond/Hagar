using System;
using System.Buffers;
using System.Buffers.Binary;
#if NETCOREAPP
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, long value) where TBufferWriter : IBufferWriter<byte> => writer.WriteVarInt(ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, byte value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.EnsureContiguous(sizeof(ushort));

            var span = writer.WritableSpan;
            var neededBytes = BitOperations.Log2(value) / 7;

            ushort lower = value;
            lower <<= 1;
            lower |= 0x01;
            lower <<= neededBytes;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), lower);
            writer.AdvanceSpan(neededBytes + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt<TBufferWriter>(ref this Writer<TBufferWriter> writer, ushort value) where TBufferWriter : IBufferWriter<byte>
        {
            writer.EnsureContiguous(sizeof(uint));

            var span = writer.WritableSpan;
            var neededBytes = BitOperations.Log2(value) / 7;

            uint lower = value;
            lower <<= 1;
            lower |= 0x01;
            lower <<= neededBytes;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), lower);
            writer.AdvanceSpan(neededBytes + 1);
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