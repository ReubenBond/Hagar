using System.Runtime.CompilerServices;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Utilities
{
    public static class VarIntReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadUInt8(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return (byte) reader.ReadVarUInt32();
                case WireType.Fixed32:
                    return (byte) reader.ReadUInt32();
                case WireType.Fixed64:
                    return (byte) reader.ReadUInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<byte>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return (ushort) reader.ReadVarUInt32();
                case WireType.Fixed32:
                    return (ushort) reader.ReadUInt32();
                case WireType.Fixed64:
                    return (ushort) reader.ReadUInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ushort>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return reader.ReadVarUInt32();
                case WireType.Fixed32:
                    return reader.ReadUInt32();
                case WireType.Fixed64:
                    return (uint) reader.ReadUInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<uint>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return reader.ReadVarUInt64();
                case WireType.Fixed32:
                    return reader.ReadUInt32();
                case WireType.Fixed64:
                    return reader.ReadUInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ulong>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadInt8(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode((byte) reader.ReadVarUInt32());
                case WireType.Fixed32:
                    return (sbyte) reader.ReadInt32();
                case WireType.Fixed64:
                    return (sbyte) reader.ReadInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<sbyte>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode((ushort) reader.ReadVarUInt32());
                case WireType.Fixed32:
                    return (short) reader.ReadInt32();
                case WireType.Fixed64:
                    return (short) reader.ReadInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<short>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(reader.ReadVarUInt32());
                case WireType.Fixed32:
                    return reader.ReadInt32();
                case WireType.Fixed64:
                    return (int) reader.ReadInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<int>(nameof(wireType));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this ref Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(reader.ReadVarUInt64());
                case WireType.Fixed32:
                    return reader.ReadInt32();
                case WireType.Fixed64:
                    return reader.ReadInt64();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<long>(nameof(wireType));
            }
        }

        private const sbyte Int8Msb = unchecked((sbyte) 0x80);
        private const short Int16Msb = unchecked((short) 0x8000);
        private const int Int32Msb = unchecked((int) 0x80000000);
        private const long Int64Msb = unchecked((long) 0x8000000000000000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte ZigZagDecode(byte encoded)
        {
            var value = (sbyte) encoded;
            return (sbyte) (-(value & 0x01) ^ ((sbyte) (value >> 1) & ~Int8Msb));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short ZigZagDecode(ushort encoded)
        {
            var value = (short) encoded;
            return (short) (-(value & 0x01) ^ ((short) (value >> 1) & ~Int16Msb));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZigZagDecode(uint encoded)
        {
            var value = (int) encoded;
            return -(value & 0x01) ^ ((value >> 1) & ~Int32Msb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ZigZagDecode(ulong encoded)
        {
            var value = (long) encoded;
            return -(value & 0x01L) ^ ((value >> 1) & ~Int64Msb);
        }
    }
}