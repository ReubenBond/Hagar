using Hagar.Buffers;
using Hagar.WireProtocol;
using System.Runtime.CompilerServices;

namespace Hagar.Utilities
{
    public static class VarIntReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadUInt8(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => (byte)reader.ReadVarUInt32(),
            WireType.Fixed32 => (byte)reader.ReadUInt32(),
            WireType.Fixed64 => (byte)reader.ReadUInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<byte>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => (ushort)reader.ReadVarUInt32(),
            WireType.Fixed32 => (ushort)reader.ReadUInt32(),
            WireType.Fixed64 => (ushort)reader.ReadUInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<ushort>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => reader.ReadVarUInt32(),
            WireType.Fixed32 => reader.ReadUInt32(),
            WireType.Fixed64 => (uint)reader.ReadUInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<uint>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => reader.ReadVarUInt64(),
            WireType.Fixed32 => reader.ReadUInt32(),
            WireType.Fixed64 => reader.ReadUInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<ulong>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadInt8(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => ZigZagDecode((byte)reader.ReadVarUInt32()),
            WireType.Fixed32 => (sbyte)reader.ReadInt32(),
            WireType.Fixed64 => (sbyte)reader.ReadInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<sbyte>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => ZigZagDecode((ushort)reader.ReadVarUInt32()),
            WireType.Fixed32 => (short)reader.ReadInt32(),
            WireType.Fixed64 => (short)reader.ReadInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<short>(nameof(wireType)),
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ref Reader reader, WireType wireType)
        {
            if (wireType == WireType.VarInt)
            {
                return ZigZagDecode(reader.ReadVarUInt32());
            }

            return ReadInt32Slower(ref reader, wireType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadInt32Slower(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.Fixed32 => reader.ReadInt32(),
            WireType.Fixed64 => (int)reader.ReadInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<int>(nameof(wireType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this ref Reader reader, WireType wireType) => wireType switch
        {
            WireType.VarInt => ZigZagDecode(reader.ReadVarUInt64()),
            WireType.Fixed32 => reader.ReadInt32(),
            WireType.Fixed64 => reader.ReadInt64(),
            _ => ExceptionHelper.ThrowArgumentOutOfRange<long>(nameof(wireType)),
        };

        private const sbyte Int8Msb = unchecked((sbyte)0x80);
        private const short Int16Msb = unchecked((short)0x8000);
        private const int Int32Msb = unchecked((int)0x80000000);
        private const long Int64Msb = unchecked((long)0x8000000000000000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte ZigZagDecode(byte encoded)
        {
            var value = (sbyte)encoded;
            return (sbyte)(-(value & 0x01) ^ ((sbyte)(value >> 1) & ~Int8Msb));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short ZigZagDecode(ushort encoded)
        {
            var value = (short)encoded;
            return (short)(-(value & 0x01) ^ ((short)(value >> 1) & ~Int16Msb));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZigZagDecode(uint encoded)
        {
            var value = (int)encoded;
            return -(value & 0x01) ^ ((value >> 1) & ~Int32Msb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ZigZagDecode(ulong encoded)
        {
            var value = (long)encoded;
            return -(value & 0x01L) ^ ((value >> 1) & ~Int64Msb);
        }
    }
}