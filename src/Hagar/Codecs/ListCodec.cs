using System;
using System.Collections.Generic;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ListCodec<T> : IFieldCodec<List<T>>
    {
        private readonly IFieldCodec<T> fieldCodec;
        private readonly ListActivator<T> activator;

        public ListCodec(IFieldCodec<T> fieldCodec, ListActivator<T> activator)
        {
            this.fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
            this.activator = activator;
        }

        void IFieldCodec<List<T>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, List<T> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, 0, typeof(int), value.Count);
            var first = true;
            foreach (var element in value)
            {
                this.fieldCodec.WriteField(ref writer, first ? 1U : 0, typeof(T), element);
                first = false;
            }

            
            writer.WriteEndObject();
        }

        List<T> IFieldCodec<List<T>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<List<T>>(ref reader, field);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            List<T> result = null;
            uint fieldId = 0;
            var length = 0;
            var index = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        length = Int32Codec.ReadValue(ref reader, header);
                        result = this.activator.Create(length);
                        result.Capacity = length;
                        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result is null) ThrowLengthFieldMissing();
                        if (index >= length) ThrowIndexOutOfRangeException(length);
                        // ReSharper disable once PossibleNullReferenceException
                        result.Add(this.fieldCodec.ReadValue(ref reader, header));
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

        private static void ThrowIndexOutOfRangeException(int length) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(List<T>)} with declared length {length}.");

        private static void ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its length field.");
    }
}
