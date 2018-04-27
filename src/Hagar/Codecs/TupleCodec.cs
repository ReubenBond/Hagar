using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class TupleCodec<T> : IFieldCodec<Tuple<T>>
    {
        private readonly IFieldCodec<T> valueCodec;

        public TupleCodec(IFieldCodec<T> valueCodec)
        {
            this.valueCodec = valueCodec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.valueCodec.WriteField(writer, session, 0, typeof(T), value.Item1);

            writer.WriteEndObject();
        }

        public Tuple<T> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.valueCodec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T>(item1);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for string fields. {field}");
    }

    public class TupleCodec<T1, T2> : IFieldCodec<Tuple<T1, T2>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;

        public TupleCodec(IFieldCodec<T1> item1Codec, IFieldCodec<T2> item2Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T1);
            var item2 = default(T2);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2>(item1, item2);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3> : IFieldCodec<Tuple<T1, T2, T3>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3>(item1, item2, item3);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4> : IFieldCodec<Tuple<T1, T2, T3, T4>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;
        private readonly IFieldCodec<T4> item4Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
            this.item4Codec = item4Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);
            this.item4Codec.WriteField(writer, session, 1, typeof(T4), value.Item4);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3, T4> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    case 4:
                        item4 = this.item4Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5> : IFieldCodec<Tuple<T1, T2, T3, T4, T5>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;
        private readonly IFieldCodec<T4> item4Codec;
        private readonly IFieldCodec<T5> item5Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
            this.item4Codec = item4Codec;
            this.item5Codec = item5Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4, T5> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);
            this.item4Codec.WriteField(writer, session, 1, typeof(T4), value.Item4);
            this.item5Codec.WriteField(writer, session, 1, typeof(T5), value.Item5);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3, T4, T5> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    case 4:
                        item4 = this.item4Codec.ReadValue(reader, session, header);
                        break;
                    case 5:
                        item5 = this.item5Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;
        private readonly IFieldCodec<T4> item4Codec;
        private readonly IFieldCodec<T5> item5Codec;
        private readonly IFieldCodec<T6> item6Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
            this.item4Codec = item4Codec;
            this.item5Codec = item5Codec;
            this.item6Codec = item6Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);
            this.item4Codec.WriteField(writer, session, 1, typeof(T4), value.Item4);
            this.item5Codec.WriteField(writer, session, 1, typeof(T5), value.Item5);
            this.item6Codec.WriteField(writer, session, 1, typeof(T6), value.Item6);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3, T4, T5, T6> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    case 4:
                        item4 = this.item4Codec.ReadValue(reader, session, header);
                        break;
                    case 5:
                        item5 = this.item5Codec.ReadValue(reader, session, header);
                        break;
                    case 6:
                        item6 = this.item6Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6, T7> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;
        private readonly IFieldCodec<T4> item4Codec;
        private readonly IFieldCodec<T5> item5Codec;
        private readonly IFieldCodec<T6> item6Codec;
        private readonly IFieldCodec<T7> item7Codec;

        public TupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec,
            IFieldCodec<T7> item7Codec)
        {
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
            this.item4Codec = item4Codec;
            this.item5Codec = item5Codec;
            this.item6Codec = item6Codec;
            this.item7Codec = item7Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);
            this.item4Codec.WriteField(writer, session, 1, typeof(T4), value.Item4);
            this.item5Codec.WriteField(writer, session, 1, typeof(T5), value.Item5);
            this.item6Codec.WriteField(writer, session, 1, typeof(T6), value.Item6);
            this.item7Codec.WriteField(writer, session, 1, typeof(T7), value.Item7);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
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
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    case 4:
                        item4 = this.item4Codec.ReadValue(reader, session, header);
                        break;
                    case 5:
                        item5 = this.item5Codec.ReadValue(reader, session, header);
                        break;
                    case 6:
                        item6 = this.item6Codec.ReadValue(reader, session, header);
                        break;
                    case 7:
                        item7 = this.item7Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }

    public class TupleCodec<T1, T2, T3, T4, T5, T6, T7, T8> : IFieldCodec<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> where T8 : struct
    {
        private readonly IFieldCodec<T1> item1Codec;
        private readonly IFieldCodec<T2> item2Codec;
        private readonly IFieldCodec<T3> item3Codec;
        private readonly IFieldCodec<T4> item4Codec;
        private readonly IFieldCodec<T5> item5Codec;
        private readonly IFieldCodec<T6> item6Codec;
        private readonly IFieldCodec<T7> item7Codec;
        private readonly IFieldCodec<T8> item8Codec;

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
            this.item1Codec = item1Codec;
            this.item2Codec = item2Codec;
            this.item3Codec = item3Codec;
            this.item4Codec = item4Codec;
            this.item5Codec = item5Codec;
            this.item6Codec = item6Codec;
            this.item7Codec = item7Codec;
            this.item8Codec = item8Codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.item1Codec.WriteField(writer, session, 0, typeof(T1), value.Item1);
            this.item2Codec.WriteField(writer, session, 1, typeof(T2), value.Item2);
            this.item3Codec.WriteField(writer, session, 1, typeof(T3), value.Item3);
            this.item4Codec.WriteField(writer, session, 1, typeof(T4), value.Item4);
            this.item5Codec.WriteField(writer, session, 1, typeof(T5), value.Item5);
            this.item6Codec.WriteField(writer, session, 1, typeof(T6), value.Item6);
            this.item7Codec.WriteField(writer, session, 1, typeof(T7), value.Item7);
            this.item8Codec.WriteField(writer, session, 1, typeof(T8), value.Rest);

            writer.WriteEndObject();
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7, T8> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
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
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        item1 = this.item1Codec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        item2 = this.item2Codec.ReadValue(reader, session, header);
                        break;
                    case 3:
                        item3 = this.item3Codec.ReadValue(reader, session, header);
                        break;
                    case 4:
                        item4 = this.item4Codec.ReadValue(reader, session, header);
                        break;
                    case 5:
                        item5 = this.item5Codec.ReadValue(reader, session, header);
                        break;
                    case 6:
                        item6 = this.item6Codec.ReadValue(reader, session, header);
                        break;
                    case 7:
                        item7 = this.item7Codec.ReadValue(reader, session, header);
                        break;
                    case 8:
                        item8 = this.item8Codec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            var result = new Tuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited}. {field}");
    }
}
