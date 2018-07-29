using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class BoolCodec : TypedCodecBase<bool, BoolCodec>, IFieldCodec<bool>
    {
        void IFieldCodec<bool>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            bool value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(bool), WireType.VarInt);
            writer.WriteVarInt(value ? 1 : 0);
        }

        bool IFieldCodec<bool>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadUInt8(field.WireType) == 1;
        }
    }
    
    public class CharCodec : TypedCodecBase<char, CharCodec>, IFieldCodec<char>
    {
        void IFieldCodec<char>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            char value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(char), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        char IFieldCodec<char>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return (char)reader.ReadUInt8(field.WireType);
        }
    }

    public class ByteCodec : TypedCodecBase<byte, ByteCodec>, IFieldCodec<byte>
    {
        void IFieldCodec<byte>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            byte value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(byte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        byte IFieldCodec<byte>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadUInt8(field.WireType);
        }
    }

    public class SByteCodec : TypedCodecBase<sbyte, SByteCodec>, IFieldCodec<sbyte>
    {
        void IFieldCodec<sbyte>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            sbyte value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(sbyte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        sbyte IFieldCodec<sbyte>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadInt8(field.WireType);
        }
    }

    public class UInt16Codec : TypedCodecBase<ushort, UInt16Codec>, IFieldCodec<ushort>
    {
        ushort IFieldCodec<ushort>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadUInt16(field.WireType);
        }

        void IFieldCodec<ushort>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            ushort value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(ushort), WireType.VarInt);
            writer.WriteVarInt(value);
        }
    }

    public class Int16Codec : TypedCodecBase<short, Int16Codec>, IFieldCodec<short>
    {
        void IFieldCodec<short>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            short value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(short), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        short IFieldCodec<short>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadInt16(field.WireType);
        }
    }

    public class UInt32Codec : TypedCodecBase<uint, UInt32Codec>, IFieldCodec<uint>
    {
        void IFieldCodec<uint>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            uint value)
        {
            ReferenceCodec.MarkValueField(session);
            if (value > 1 << 20)
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(uint), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(uint), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        uint IFieldCodec<uint>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadUInt32(field.WireType);
        }
    }

    public class Int32Codec : TypedCodecBase<int, Int32Codec>, IFieldCodec<int>
    {
        void IFieldCodec<int>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            int value)
        {
            ReferenceCodec.MarkValueField(session);
            if (value > 1 << 20 || -value > 1 << 20)
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(int), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(int), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        int IFieldCodec<int>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadInt32(field.WireType);
        }
    }

    public class Int64Codec : TypedCodecBase<long, Int64Codec>, IFieldCodec<long>
    {
        void IFieldCodec<long>.WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, long value)
        {
            ReferenceCodec.MarkValueField(session);
            if (value <= int.MaxValue && value >= int.MinValue)
            {
                if (value > 1 << 20 || -value > 1 << 20)
                {
                    writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(long), WireType.Fixed32);
                    writer.Write((int)value);
                }
                else
                {
                    writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(long), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41 || -value > 1 << 41)
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(long), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(long), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        long IFieldCodec<long>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadInt64(field.WireType);
        }
    }

    public class UInt64Codec : TypedCodecBase<ulong, UInt64Codec>, IFieldCodec<ulong>
    {
        void IFieldCodec<ulong>.WriteField(
            ref Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            ulong value)
        {
            ReferenceCodec.MarkValueField(session);
            if (value <= int.MaxValue)
            {
                if (value > 1 << 20)
                {
                    writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(ulong), WireType.Fixed32);
                    writer.Write((uint)value);
                }
                else
                {
                    writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(ulong), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41)
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(ulong), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(ulong), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        ulong IFieldCodec<ulong>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            return reader.ReadUInt64(field.WireType);
        }
    }
}