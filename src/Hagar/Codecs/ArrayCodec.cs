using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for arrays of rank 1.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ArrayCodec<T> : IFieldCodec<T[]>
    {
        private readonly IFieldCodec<T> _fieldCodec;

        public ArrayCodec(IFieldCodec<T> fieldCodec)
        {
            _fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, T[] value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, 0, typeof(int), value.Length);
            var first = true;
            foreach (var element in value)
            {
                _fieldCodec.WriteField(ref writer, first ? 1U : 0, typeof(T), element);
                first = false;
            }

            writer.WriteEndObject();
        }

        public T[] ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<T[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            T[] result = null;
            uint fieldId = 0;
            var length = 0;
            var index = 0;
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
                        length = Int32Codec.ReadValue(ref reader, header);
                        result = new T[length];
                        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result is null)
                        {
                            return ThrowLengthFieldMissing();
                        }

                        if (index >= length)
                        {
                            return ThrowIndexOutOfRangeException(length);
                        }

                        result[index] = _fieldCodec.ReadValue(ref reader, header);
                        ++index;
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }

    /// <summary>
    /// Codec for <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ReadOnlyMemoryCodec<T> : IFieldCodec<ReadOnlyMemory<T>>
    {
        private readonly IFieldCodec<T> _fieldCodec;

        public ReadOnlyMemoryCodec(IFieldCodec<T> fieldCodec)
        {
            _fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ReadOnlyMemory<T> value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, 0, typeof(int), value.Length);
            var first = true;
            foreach (var element in value.Span)
            {
                _fieldCodec.WriteField(ref writer, first ? 1U : 0, typeof(T), element);
                first = false;
            }

            writer.WriteEndObject();
        }

        public ReadOnlyMemory<T> ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<T[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            T[] result = null;
            uint fieldId = 0;
            var length = 0;
            var index = 0;
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
                        length = Int32Codec.ReadValue(ref reader, header);
                        result = new T[length];
                        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result is null)
                        {
                            return ThrowLengthFieldMissing();
                        }

                        if (index >= length)
                        {
                            return ThrowIndexOutOfRangeException(length);
                        }

                        result[index] = _fieldCodec.ReadValue(ref reader, header);
                        ++index;
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }
    
    /// <summary>
    /// Codec for <see cref="Memory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class MemoryCodec<T> : IFieldCodec<Memory<T>>
    {
        private readonly IFieldCodec<T> _fieldCodec;

        public MemoryCodec(IFieldCodec<T> fieldCodec)
        {
            _fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Memory<T> value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, 0, typeof(int), value.Length);
            var first = true;
            foreach (var element in value.Span)
            {
                _fieldCodec.WriteField(ref writer, first ? 1U : 0, typeof(T), element);
                first = false;
            }

            writer.WriteEndObject();
        }

        public Memory<T> ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<T[], TInput>(ref reader, field);
            }

            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            T[] result = null;
            uint fieldId = 0;
            var length = 0;
            var index = 0;
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
                        length = Int32Codec.ReadValue(ref reader, header);
                        result = new T[length];
                        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result is null)
                        {
                            return ThrowLengthFieldMissing();
                        }

                        if (index >= length)
                        {
                            return ThrowIndexOutOfRangeException(length);
                        }

                        result[index] = _fieldCodec.ReadValue(ref reader, header);
                        ++index;
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }
}