using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;

namespace Hagar.Codecs
{
    public sealed class TupleCodec<T> : IFieldCodec<Tuple<T>>
    {
        private readonly IFieldCodec<T> _valueCodec;

        public TupleCodec(IFieldCodec<T> valueCodec)
        {
            _valueCodec = HagarGeneratedCodeHelper.UnwrapService(this, valueCodec);
        }

        void IFieldCodec<Tuple<T>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Tuple<T> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _valueCodec.WriteField(ref writer, 0, typeof(T), value.Item1);

            writer.WriteEndObject();
        }

        Tuple<T> IFieldCodec<Tuple<T>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T>(item1);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for string fields. {field}");
    }

    public class TupleCodec<T1, T2> : IFieldCodec<Tuple<T1, T2>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;

        public TupleCodec(IFieldCodec<T1> item1Codec, IFieldCodec<T2> item2Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
        }

        void IFieldCodec<Tuple<T1, T2>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Tuple<T1, T2> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);

            writer.WriteEndObject();
        }

        Tuple<T1, T2> IFieldCodec<Tuple<T1, T2>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2>(item1, item2);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3> : IFieldCodec<Tuple<T1, T2, T3>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec)
        {
            _item1Codec = HagarGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = HagarGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = HagarGeneratedCodeHelper.UnwrapService(this, item3Codec);
        }

        void IFieldCodec<Tuple<T1, T2, T3>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);

            writer.WriteEndObject();
        }

        Tuple<T1, T2, T3> IFieldCodec<Tuple<T1, T2, T3>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3>(item1, item2, item3);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4> : IFieldCodec<Tuple<T1, T2, T3, T4>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;

        public TupleCodec(
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

        void IFieldCodec<Tuple<T1, T2, T3, T4>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);

            writer.WriteEndObject();
        }

        Tuple<T1, T2, T3, T4> IFieldCodec<Tuple<T1, T2, T3, T4>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5> : IFieldCodec<Tuple<T1, T2, T3, T4, T5>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;

        public TupleCodec(
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

        void IFieldCodec<Tuple<T1, T2, T3, T4, T5>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            Tuple<T1, T2, T3, T4, T5> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);

            writer.WriteEndObject();
        }

        Tuple<T1, T2, T3, T4, T5> IFieldCodec<Tuple<T1, T2, T3, T4, T5>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;

        public TupleCodec(
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

        void IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 0, typeof(T1), value.Item1);
            _item2Codec.WriteField(ref writer, 1, typeof(T2), value.Item2);
            _item3Codec.WriteField(ref writer, 1, typeof(T3), value.Item3);
            _item4Codec.WriteField(ref writer, 1, typeof(T4), value.Item4);
            _item5Codec.WriteField(ref writer, 1, typeof(T5), value.Item5);
            _item6Codec.WriteField(ref writer, 1, typeof(T6), value.Item6);

            writer.WriteEndObject();
        }

        Tuple<T1, T2, T3, T4, T5, T6> IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6, T7> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;

        public TupleCodec(
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

        void IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

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

        Tuple<T1, T2, T3, T4, T5, T6, T7> IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6, T7, T8> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> where T8 : struct
    {
        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;
        private readonly IFieldCodec<T8> _item8Codec;

        public TupleCodec(
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

        void IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            Tuple<T1, T2, T3, T4, T5, T6, T7, T8> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

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

        Tuple<T1, T2, T3, T4, T5, T6, T7, T8> IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
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

            var result = new Tuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }
}