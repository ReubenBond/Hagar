using Hagar.Buffers;
using Hagar.Utilities;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hagar.Codecs
{
    public sealed class BoolCodec : TypedCodecBase<bool, BoolCodec>, IFieldCodec<bool>
    {
        void IFieldCodec<bool>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            bool value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, bool value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(bool), WireType.VarInt);
            writer.WriteVarInt(value ? 1 : 0);
        }

        bool IFieldCodec<bool>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadUInt8(field.WireType) == 1;
        }
    }

    public sealed class CharCodec : TypedCodecBase<char, CharCodec>, IFieldCodec<char>
    {
        void IFieldCodec<char>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            char value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, char value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(char), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        char IFieldCodec<char>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return (char)reader.ReadUInt16(field.WireType);
        }
    }

    public sealed class ByteCodec : TypedCodecBase<byte, ByteCodec>, IFieldCodec<byte>
    {
        void IFieldCodec<byte>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            byte value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, byte value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(byte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        byte IFieldCodec<byte>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadUInt8(field.WireType);
        }
    }

    public sealed class SByteCodec : TypedCodecBase<sbyte, SByteCodec>, IFieldCodec<sbyte>
    {
        void IFieldCodec<sbyte>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            sbyte value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, sbyte value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(sbyte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        sbyte IFieldCodec<sbyte>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadInt8(field.WireType);
        }
    }

    public sealed class UInt16Codec : TypedCodecBase<ushort, UInt16Codec>, IFieldCodec<ushort>
    {
        ushort IFieldCodec<ushort>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadUInt16(field.WireType);
        }

        void IFieldCodec<ushort>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ushort value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ushort value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ushort), WireType.VarInt);
            writer.WriteVarInt(value);
        }
    }

    public sealed class Int16Codec : TypedCodecBase<short, Int16Codec>, IFieldCodec<short>
    {
        void IFieldCodec<short>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            short value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, short value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(short), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        short IFieldCodec<short>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadInt16(field.WireType);
        }
    }

    public sealed class UInt32Codec : TypedCodecBase<uint, UInt32Codec>, IFieldCodec<uint>
    {
        void IFieldCodec<uint>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            uint value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, uint value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            if (value > 1 << 20)
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(uint), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(uint), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        uint IFieldCodec<uint>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadUInt32(field.WireType);
        }
    }

    public sealed class Int32Codec : TypedCodecBase<int, Int32Codec>, IFieldCodec<int>
    {
        void IFieldCodec<int>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            int value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            if (value > 1 << 20 || -value > 1 << 20)
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(int), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(int), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        public static void WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            int value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            if (value > 1 << 20 || -value > 1 << 20)
            {
                writer.WriteFieldHeaderExpected(fieldIdDelta, WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeaderExpected(fieldIdDelta, WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        int IFieldCodec<int>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadInt32(field.WireType);
        }
    }

    public sealed class Int64Codec : TypedCodecBase<long, Int64Codec>, IFieldCodec<long>
    {
        void IFieldCodec<long>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, long value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, long value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            if (value <= int.MaxValue && value >= int.MinValue)
            {
                if (value > 1 << 20 || -value > 1 << 20)
                {
                    writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(long), WireType.Fixed32);
                    writer.Write((int)value);
                }
                else
                {
                    writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(long), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41 || -value > 1 << 41)
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(long), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(long), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        long IFieldCodec<long>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadInt64(field.WireType);
        }
    }

    public sealed class UInt64Codec : TypedCodecBase<ulong, UInt64Codec>, IFieldCodec<ulong>
    {
        void IFieldCodec<ulong>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ulong value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ulong value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            if (value <= int.MaxValue)
            {
                if (value > 1 << 20)
                {
                    writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ulong), WireType.Fixed32);
                    writer.Write((uint)value);
                }
                else
                {
                    writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ulong), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41)
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ulong), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ulong), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        ulong IFieldCodec<ulong>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            return reader.ReadUInt64(field.WireType);
        }
    }
}