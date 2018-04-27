using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Utilities
{
    public static class VarIntReaderExtensions
    {
        public static byte ReadVarUInt8(this Reader reader)
        {
            var next = reader.ReadByte();
            if ((next & 0x80) == 0) return next;
            var result = (byte) (next & 0x7F);

            next = reader.ReadByte();
            result |= (byte)((next & 0x7F) << 7);

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte(); 

            return result;
        }

        public static ushort ReadVarUInt16(this Reader reader)
        {
            var next = reader.ReadByte();
            var result = (ushort)(next & 0x7F);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (ushort)((next & 0x7F) << 7);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (ushort)((next & 0x7F) << 14);

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static uint ReadVarUInt32(this Reader reader)
        {
            var next = reader.ReadByte();
            var result = (uint)(next & 0x7F);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 7);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 14);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 21);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 28);
            if ((next & 0x80) == 0) return result;

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static int GetVarIntLength(this Reader reader)
        {
            var count = 1;
            while (true)
            {
                var next = reader.ReadByte();
                if ((next & 0x80) == 0) return count;
                count++;
            }
        }

        public static ulong ReadVarUInt64(this Reader reader)
        {
            ulong next = reader.ReadByte();
            var result = next & 0x7F;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 7;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 14;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 21;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 28;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 35;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 42;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 49;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 56;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 63;
            if ((next & 0x80) == 0) return result;

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static byte ReadUInt8(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt8(reader);
                case WireType.Fixed32:
                    return (byte) reader.ReadUInt();
                case WireType.Fixed64:
                    return (byte) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<byte>(nameof(wireType));
            }
        }

        public static ushort ReadUInt16(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt16(reader);
                case WireType.Fixed32:
                    return (ushort) reader.ReadUInt();
                case WireType.Fixed64:
                    return (ushort) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ushort>(nameof(wireType));
            }
        }

        public static uint ReadUInt32(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt32(reader);
                case WireType.Fixed32:
                    return reader.ReadUInt();
                case WireType.Fixed64:
                    return (uint) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<uint>(nameof(wireType));
            }
        }

        public static ulong ReadUInt64(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt64(reader);
                case WireType.Fixed32:
                    return reader.ReadUInt();
                case WireType.Fixed64:
                    return reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ulong>(nameof(wireType));
            }
        }

        public static sbyte ReadInt8(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt8(reader));
                case WireType.Fixed32:
                    return (sbyte) reader.ReadInt();
                case WireType.Fixed64:
                    return (sbyte) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<sbyte>(nameof(wireType));
            }
        }

        public static short ReadInt16(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt16(reader));
                case WireType.Fixed32:
                    return (short) reader.ReadInt();
                case WireType.Fixed64:
                    return (short) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<short>(nameof(wireType));
            }
        }

        public static int ReadInt32(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt32(reader));
                case WireType.Fixed32:
                    return reader.ReadInt();
                case WireType.Fixed64:
                    return (int) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<int>(nameof(wireType));
            }
        }

        public static long ReadInt64(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt64(reader));
                case WireType.Fixed32:
                    return reader.ReadInt();
                case WireType.Fixed64:
                    return reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<long>(nameof(wireType));
            }
        }

        private const sbyte Int8Msb = unchecked((sbyte) 0x80);
        private const short Int16Msb = unchecked((short) 0x8000);
        private const int Int32Msb = unchecked((int) 0x80000000);
        private const long Int64Msb = unchecked((long) 0x8000000000000000);

        private static sbyte ZigZagDecode(byte encoded)
        {
            var value = (sbyte) encoded;
            return (sbyte) (-(value & 0x01) ^ ((sbyte) (value >> 1) & ~Int8Msb));
        }

        private static short ZigZagDecode(ushort encoded)
        {
            var value = (short) encoded;
            return (short) (-(value & 0x01) ^ ((short) (value >> 1) & ~Int16Msb));
        }

        private static int ZigZagDecode(uint encoded)
        {
            var value = (int) encoded;
            return -(value & 0x01) ^ ((value >> 1) & ~Int32Msb);
        }

        private static long ZigZagDecode(ulong encoded)
        {
            var value = (long) encoded;
            return -(value & 0x01L) ^ ((value >> 1) & ~Int64Msb);
        }
    }
}