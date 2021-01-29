using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class FloatCodec : TypedCodecBase<float, FloatCodec>, IFieldCodec<float>
    {
        private const int DecimalWidth = 16;

        void IFieldCodec<float>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            float value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, float value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(float), WireType.Fixed32);

            // TODO: Optimize
            writer.Write((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        float IFieldCodec<float>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static float ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            switch (field.WireType)
            {
                case WireType.Fixed32:
                    return reader.ReadFloat();
                case WireType.Fixed64:
                    {
                        var value = reader.ReadDouble();
                        if ((value > float.MaxValue || value < float.MinValue) && !double.IsInfinity(value) && !double.IsNaN(value))
                        {
                            ThrowValueOutOfRange(value);
                        }

                        return (float)value;
                    }

                case WireType.LengthPrefixed:
                    // Decimal has a smaller range, but higher precision than float.
                    var length = reader.ReadVarUInt32();
                    if (length != DecimalWidth)
                    {
                        throw new UnexpectedLengthPrefixValueException("float", DecimalWidth, length, field.ToString());
                    }
                    return (float)reader.ReadDecimal();

                default:
                    ThrowWireTypeOutOfRange(field.WireType);
                    return 0;
            }
        }

        private static void ThrowWireTypeOutOfRange(WireType wireType) => throw new ArgumentOutOfRangeException(
            $"{nameof(wireType)} {wireType} is not supported by this codec.");

        private static void ThrowValueOutOfRange<T>(T value) => throw new ArgumentOutOfRangeException(
            $"The {typeof(T)} value has a magnitude too high {value} to be converted to {typeof(float)}.");
    }

    [RegisterSerializer]
    public sealed class DoubleCodec : TypedCodecBase<double, DoubleCodec>, IFieldCodec<double>
    {
        private const int DecimalWidth = 16;

        void IFieldCodec<double>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            double value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, double value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(double), WireType.Fixed64);

            // TODO: Optimize
            writer.Write((ulong)BitConverter.ToInt64(BitConverter.GetBytes(value), 0));
        }

        double IFieldCodec<double>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static double ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            switch (field.WireType)
            {
                case WireType.Fixed32:
                    return reader.ReadFloat();
                case WireType.Fixed64:
                    return reader.ReadDouble();
                case WireType.LengthPrefixed:
                    var length = reader.ReadVarUInt32();
                    if (length != DecimalWidth)
                    {
                        throw new UnexpectedLengthPrefixValueException("double", DecimalWidth, length, field.ToString());
                    }
                    return (double)reader.ReadDecimal();
                default:
                    ThrowWireTypeOutOfRange(field.WireType);
                    return 0;
            }
        }

        private static void ThrowWireTypeOutOfRange(WireType wireType) => throw new ArgumentOutOfRangeException(
            $"{nameof(wireType)} {wireType} is not supported by this codec.");
    }

    [RegisterSerializer]
    public sealed class DecimalCodec : TypedCodecBase<decimal, DecimalCodec>, IFieldCodec<decimal>
    {
        private const int Width = 16;

        void IFieldCodec<decimal>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, decimal value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

        public static void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, decimal value) where TBufferWriter : IBufferWriter<byte>
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(decimal), WireType.LengthPrefixed);
            writer.WriteVarInt(Width);
            var ints = decimal.GetBits(value);
            foreach (var part in ints)
            {
                writer.Write(part);
            }
        }

        decimal IFieldCodec<decimal>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => ReadValue(ref reader, field);

        public static decimal ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            switch (field.WireType)
            {
                case WireType.Fixed32:
                    {
                        var value = reader.ReadFloat();
                        if (value > (float)decimal.MaxValue || value < (float)decimal.MinValue)
                        {
                            ThrowValueOutOfRange(value);
                        }

                        return (decimal)value;
                    }
                case WireType.Fixed64:
                    {
                        var value = reader.ReadDouble();
                        if (value > (double)decimal.MaxValue || value < (double)decimal.MinValue)
                        {
                            ThrowValueOutOfRange(value);
                        }

                        return (decimal)value;
                    }
                case WireType.LengthPrefixed:
                    var length = reader.ReadVarUInt32();
                    if (length != Width)
                    {
                        throw new UnexpectedLengthPrefixValueException("decimal", Width, length, field.ToString());
                    }
                    return reader.ReadDecimal();
                default:
                    ThrowWireTypeOutOfRange(field.WireType);
                    return 0;
            }
        }

        private static void ThrowWireTypeOutOfRange(WireType wireType) => throw new ArgumentOutOfRangeException(
            $"{nameof(wireType)} {wireType} is not supported by this codec.");

        private static void ThrowValueOutOfRange<T>(T value) => throw new ArgumentOutOfRangeException(
            $"The {typeof(T)} value has a magnitude too high {value} to be converted to {typeof(decimal)}.");

    }
}