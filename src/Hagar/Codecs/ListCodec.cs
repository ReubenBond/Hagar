using Hagar.Activators;
using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class ListCodec<T> : IFieldCodec<List<T>>
    {
        private static readonly Type CodecElementType = typeof(T);

        private readonly IFieldCodec<T> _fieldCodec;
        private readonly ListActivator<T> _activator;

        public ListCodec(IFieldCodec<T> fieldCodec, ListActivator<T> activator)
        {
            _fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
            _activator = activator;
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, List<T> value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, 0, Int32Codec.CodecFieldType, value.Count);
            uint innerFieldIdDelta = 1;
            foreach (var element in value)
            {
                _fieldCodec.WriteField(ref writer, innerFieldIdDelta, CodecElementType, element);
                innerFieldIdDelta = 0;
            }

            writer.WriteEndObject();
        }

        public List<T> ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<List<T>, TInput>(ref reader, field);
            }

            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            List<T> result = null;
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
                        result = _activator.Create(length);
                        result.Capacity = length;
                        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result is null)
                        {
                            ThrowLengthFieldMissing();
                        }

                        if (index >= length)
                        {
                            ThrowIndexOutOfRangeException(length);
                        }
                        // ReSharper disable once PossibleNullReferenceException
                        result.Add(_fieldCodec.ReadValue(ref reader, header));
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
        private static void ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(List<T>)} with declared length {length}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }
}