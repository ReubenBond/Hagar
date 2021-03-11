using Hagar.Buffers;
using Hagar.Cloning;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for arrays of rank 1.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class ArrayCodec<T> : IFieldCodec<T[]>
    {
        private readonly IFieldCodec<T> _fieldCodec;
        private static readonly Type CodecElementType = typeof(T);

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

            Int32Codec.WriteField(ref writer, 0, Int32Codec.CodecFieldType, value.Length);
            uint innerFieldIdDelta = 1;
            foreach (var element in value)
            {
                _fieldCodec.WriteField(ref writer, innerFieldIdDelta, CodecElementType, element);
                innerFieldIdDelta = 0;
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
                        if (length > 10240 && length > reader.Length)
                        {
                            ThrowInvalidSizeException(length);
                        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidSizeException(int length) => throw new IndexOutOfRangeException(
            $"Declared length of {typeof(T[])}, {length}, is greater than total length of input.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }

    [RegisterCopier]
    public sealed class ArrayCopier<T> : IDeepCopier<T[]>
    {
        private readonly IDeepCopier<T> _elementCopier;

        public ArrayCopier(IDeepCopier<T> elementCopier)
        {
            _elementCopier = HagarGeneratedCodeHelper.UnwrapService(this, elementCopier);
        }

        public T[] DeepCopy(T[] input, CopyContext context)
        {
            if (context.TryGetCopy<T[]>(input, out var result))
            {
                return result;
            }

            result = new T[input.Length];
            context.RecordCopy(input, result);
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = _elementCopier.DeepCopy(input[i], context);
            }

            return result;
        }
    }

    /// <summary>
    /// Codec for <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class ReadOnlyMemoryCodec<T> : IFieldCodec<ReadOnlyMemory<T>>
    {
        private static readonly Type CodecElementType = typeof(T);
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

            Int32Codec.WriteField(ref writer, 0, Int32Codec.CodecFieldType, value.Length);
            uint innerFieldIdDelta = 1;
            foreach (var element in value.Span)
            {
                _fieldCodec.WriteField(ref writer, innerFieldIdDelta, CodecElementType, element);
                innerFieldIdDelta = 0;
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
                        if (length > 10240 && length > reader.Length)
                        {
                            ThrowInvalidSizeException(length);
                        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidSizeException(int length) => throw new IndexOutOfRangeException(
            $"Declared length of {typeof(ReadOnlyMemory<T>)}, {length}, is greater than total length of input.");
    }

    [RegisterCopier]
    public sealed class ReadOnlyMemoryCopier<T> : IDeepCopier<ReadOnlyMemory<T>>
    {
        private readonly IDeepCopier<T> _elementCopier;

        public ReadOnlyMemoryCopier(IDeepCopier<T> elementCopier)
        {
            _elementCopier = HagarGeneratedCodeHelper.UnwrapService(this, elementCopier);
        }

        public ReadOnlyMemory<T> DeepCopy(ReadOnlyMemory<T> input, CopyContext context)
        {
            if (input.IsEmpty)
            {
                return input;
            }

            // Note that there is a possibility for infinite recursion here if the underlying object in the input is
            // able to take part in a cyclic reference. If we could get that object then we could prevent that cycle.
            var inputSpan = input.Span;
            var result = new T[inputSpan.Length];
            for (var i = 0; i < inputSpan.Length; i++)
            {
                result[i] = _elementCopier.DeepCopy(inputSpan[i], context);
            }

            return result;
        }
    }
    
    /// <summary>
    /// Codec for <see cref="Memory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class MemoryCodec<T> : IFieldCodec<Memory<T>>
    {
        private static readonly Type CodecElementType = typeof(T);
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

            Int32Codec.WriteField(ref writer, 0, Int32Codec.CodecFieldType, value.Length);
            uint innerFieldIdDelta = 1;
            foreach (var element in value.Span)
            {
                _fieldCodec.WriteField(ref writer, innerFieldIdDelta, CodecElementType, element);
                innerFieldIdDelta = 0;
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
                        if (length > 10240 && length > reader.Length)
                        {
                            ThrowInvalidSizeException(length);
                        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T[])} with declared length {length}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T[] ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidSizeException(int length) => throw new IndexOutOfRangeException(
            $"Declared length of {typeof(ReadOnlyMemory<T>)}, {length}, is greater than total length of input.");
    }

    [RegisterCopier]
    public sealed class MemoryCopier<T> : IDeepCopier<Memory<T>>
    {
        private readonly IDeepCopier<T> _elementCopier;

        public MemoryCopier(IDeepCopier<T> elementCopier)
        {
            _elementCopier = HagarGeneratedCodeHelper.UnwrapService(this, elementCopier);
        }

        public Memory<T> DeepCopy(Memory<T> input, CopyContext context)
        {
            if (input.IsEmpty)
            {
                return input;
            }

            // Note that there is a possibility for infinite recursion here if the underlying object in the input is
            // able to take part in a cyclic reference. If we could get that object then we could prevent that cycle.
            var inputSpan = input.Span;
            var result = new T[inputSpan.Length];
            for (var i = 0; i < inputSpan.Length; i++)
            {
                result[i] = _elementCopier.DeepCopy(inputSpan[i], context);
            }

            return result;
        }
    }
}