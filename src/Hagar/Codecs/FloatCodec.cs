using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class FloatCodec : TypedCodecBase<float, FloatCodec>, IFieldCodec<float>
    {
        void IFieldCodec<float>.WriteField(
            Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            float value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(float), WireType.Fixed32);
            writer.Write((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        float IFieldCodec<float>.ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
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

                    return (float) value;
                }

                case WireType.Fixed128:
                // Decimal has a smaller range, but higher precision than float.
                return (float) reader.ReadDecimal();

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

    public class DoubleCodec : TypedCodecBase<double, DoubleCodec>, IFieldCodec<double>
    {
        void IFieldCodec<double>.WriteField(
            Writer writer,
            SerializerSession session,
            uint fieldIdDelta,
            Type expectedType,
            double value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(double), WireType.Fixed64);
            writer.Write(value);
        }

        double IFieldCodec<double>.ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            switch (field.WireType)
            {
                case WireType.Fixed32:
                    return reader.ReadFloat();
                case WireType.Fixed64:
                    return reader.ReadDouble();
                case WireType.Fixed128:
                    return (double) reader.ReadDecimal();
                default:
                    ThrowWireTypeOutOfRange(field.WireType);
                    return 0;
            }
        }

        private static void ThrowWireTypeOutOfRange(WireType wireType) => throw new ArgumentOutOfRangeException(
            $"{nameof(wireType)} {wireType} is not supported by this codec.");
    }

    public class DecimalCodec : TypedCodecBase<decimal, DecimalCodec>, IFieldCodec<decimal>
    {
        void IFieldCodec<decimal>.WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, decimal value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(decimal), WireType.Fixed128);
            var ints = Decimal.GetBits(value);
            foreach (var part in ints) writer.Write(part);
        }

        decimal IFieldCodec<decimal>.ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            switch (field.WireType)
            {
                case WireType.Fixed32:
                {
                    var value = reader.ReadFloat();
                    if (value > (float) decimal.MaxValue || value < (float) decimal.MinValue)
                    {
                        ThrowValueOutOfRange(value);
                    }

                    return (decimal) value;
                }
                case WireType.Fixed64:
                {
                    var value = reader.ReadDouble();
                    if (value > (double) decimal.MaxValue || value < (double) decimal.MinValue)
                    {
                        ThrowValueOutOfRange(value);
                    }

                    return (decimal) value;
                }
                case WireType.Fixed128:
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