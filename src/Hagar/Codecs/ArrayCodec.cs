using System;
using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for arrays of rank 1.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal class ArrayCodec<T> : IFieldCodec<T[]>
    {
        private readonly IFieldCodec<T> fieldCodec;

        public ArrayCodec(IFieldCodec<T> fieldCodec)
        {
            this.fieldCodec = HagarGeneratedCodeHelper.UnwrapService(this, fieldCodec);
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, T[] value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            Int32Codec.WriteField(ref writer, session, 0, typeof(int), value.Length);
            var first = true;
            foreach (var element in value)
            {
                this.fieldCodec.WriteField(ref writer, session, first ? 1U : 0, typeof(T), element);
                first = false;
            }

            writer.WriteEndObject();
        }

        public T[] ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<T[]>(ref reader, session, field);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            T[] result = null;
            uint fieldId = 0;
            var length = 0;
            var index = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        length = Int32Codec.ReadValue(ref reader, session, header);
                        result = new T[length];
                        ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
                        break;
                    case 1:
                        if (result == null) return ThrowLengthFieldMissing();
                        if (index >= length) return ThrowIndexOutOfRangeException(length);
                        result[index] = this.fieldCodec.ReadValue(ref reader, session, header);
                        ++index;
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
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
