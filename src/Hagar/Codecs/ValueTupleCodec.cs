using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ValueTupleCodec : IFieldCodec<ValueTuple>
    {
        void IFieldCodec<ValueTuple>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ValueTuple value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.VarInt);
            writer.WriteVarInt(0);
        }

        ValueTuple IFieldCodec<ValueTuple>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.VarInt)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            _ = reader.ReadVarUInt64();

            return default;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.VarInt} is supported for string fields. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T> : IFieldCodec<ValueTuple<T>>
    {
        private readonly IFieldCodec<T> _valueCodec;

        public ValueTupleCodec(IFieldCodec<T> valueCodec)
        {
            _valueCodec = HagarGeneratedCodeHelper.UnwrapService(this, valueCodec);
        }

        void IFieldCodec<ValueTuple<T>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ValueTuple<T> value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _valueCodec.WriteField(ref writer, 0, typeof(T), value.Item1);

            writer.WriteEndObject();
        }

        ValueTuple<T> IFieldCodec<ValueTuple<T>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _valueCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T>(item1);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2> : IFieldCodec<ValueTuple<T1, T2>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;

        public ValueTupleCodec(IFieldCodec<T1> item1Codec, IFieldCodec<T2> item2Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);

            writer.WriteEndObject();
        }

        (T1, T2) IFieldCodec<ValueTuple<T1, T2>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2>(item1, item2);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3> : IFieldCodec<ValueTuple<T1, T2, T3>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);

            writer.WriteEndObject();
        }

        (T1, T2, T3) IFieldCodec<ValueTuple<T1, T2, T3>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3>(item1, item2, item3);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4> : IFieldCodec<ValueTuple<T1, T2, T3, T4>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = HagarGeneratedCodeHelper.UnwrapService(this, item4Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3, T4>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);

            writer.WriteEndObject();
        }

        (T1, T2, T3, T4) IFieldCodec<ValueTuple<T1, T2, T3, T4>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = HagarGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = HagarGeneratedCodeHelper.UnwrapService(this, item5Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);

            writer.WriteEndObject();
        }

        (T1, T2, T3, T4, T5) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = HagarGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = HagarGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = HagarGeneratedCodeHelper.UnwrapService(this, item6Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5, T6) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);
            _item6Codec.WriteField(ref writer, 1, typeof(T6), value.Item6);


            writer.WriteEndObject();
        }

        (T1, T2, T3, T4, T5, T6) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6, T7> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec,
            IFieldCodec<T7> item7Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = HagarGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = HagarGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = HagarGeneratedCodeHelper.UnwrapService(this, item6Codec);
            _item7Codec = HagarGeneratedCodeHelper.UnwrapService(this, item7Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5, T6, T7) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);
            _item6Codec.WriteField(ref writer, 1, typeof(T6), value.Item6);
            _item7Codec.WriteField(ref writer, 1, typeof(T7), value.Item7);


            writer.WriteEndObject();
        }

        (T1, T2, T3, T4, T5, T6, T7) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.ReadValue<TInput>(
            ref Reader<TInput> reader,
            Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            var item7 = default(T7);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    case 7:
                        item7 = _item7Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }

    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6, T7, T8> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>> where T8 : struct
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;
        private readonly IFieldCodec<T8> _item8Codec;

        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec,
            IFieldCodec<T7> item7Codec,
            IFieldCodec<T8> item8Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = HagarGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = HagarGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = HagarGeneratedCodeHelper.UnwrapService(this, item6Codec);
            _item7Codec = HagarGeneratedCodeHelper.UnwrapService(this, item7Codec);
            _item8Codec = HagarGeneratedCodeHelper.UnwrapService(this, item8Codec);
        }

        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);
            _item6Codec.WriteField(ref writer, 1, typeof(T6), value.Item6);
            _item7Codec.WriteField(ref writer, 1, typeof(T7), value.Item7);
            _item8Codec.WriteField(ref writer, 1, typeof(T8), value.Rest);

            writer.WriteEndObject();
        }

        ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.ReadValue<TInput>(ref Reader<TInput> reader,
            Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            var item7 = default(T7);
            var item8 = default(T8);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    case 7:
                        item7 = _item7Codec.ReadValue(ref reader, header);
                        break;
                    case 8:
                        item8 = _item8Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }
}