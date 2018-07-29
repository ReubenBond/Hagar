using System;
using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Serializer for multi-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    internal class MultiDimensionalArrayCodec<T> : IGeneralizedCodec
    {
        private readonly IFieldCodec<int[]> intArrayCodec;
        private readonly IFieldCodec<T> elementCodec;

        public MultiDimensionalArrayCodec(IFieldCodec<int[]> intArrayCodec, IFieldCodec<T> elementCodec)
        {
            this.intArrayCodec = HagarGeneratedCodeHelper.UnwrapService(this, intArrayCodec);
            this.elementCodec = HagarGeneratedCodeHelper.UnwrapService(this, elementCodec);
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            var array = (Array) value;
            var rank = array.Rank;

            var lengths = new int[rank];
            var indices = new int[rank];

            // Write array lengths.
            for (var i = 0; i < rank; i++)
            {
                lengths[i] = array.GetLength(i);
            }

            this.intArrayCodec.WriteField(ref writer, session, 0, typeof(int[]), lengths);

            var remaining = array.Length;
            var first = true;
            while (remaining-- > 0)
            {
                var element = array.GetValue(indices);
                this.elementCodec.WriteField(ref writer, session, first ? 1U : 0, typeof(T), (T) element);
                first = false;

                // Increment the indices array by 1.
                if (remaining > 0)
                {
                    var idx = rank - 1;
                    while (idx >= 0 && ++indices[idx] >= lengths[idx])
                    {
                        indices[idx] = 0;
                        --idx;
                        if (idx < 0) ThrowIndexOutOfRangeException(lengths);
                    }
                }
            }

            
            writer.WriteEndObject();
        }

        public object ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<T[]>(ref reader, session, field);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            Array result = null;
            uint fieldId = 0;
            int[] lengths = null;
            int[] indices = null;
            var rank = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                    {
                        lengths = this.intArrayCodec.ReadValue(ref reader, session, header);
                        rank = lengths.Length;

                        // Multi-dimensional arrays must be indexed using indexing arrays, so create one now.
                        indices = new int[rank];
                        result = Array.CreateInstance(typeof(T), lengths);
                        ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
                        break;
                    }
                    case 1:
                    {
                        if (result == null) return ThrowLengthsFieldMissing();
                        var element = this.elementCodec.ReadValue(ref reader, session, header);
                        result.SetValue(element, indices);

                        // Increment the indices array by 1.
                        var idx = rank - 1;
                        while (idx >= 0 && ++indices[idx] >= lengths[idx])
                        {
                            indices[idx] = 0;
                            --idx;
                        }

                        break;
                    }
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            return result;
        }

        public bool IsSupportedType(Type type) => type.IsArray && type.GetArrayRank() > 1;

        private static object ThrowIndexOutOfRangeException(int[] lengths) => throw new IndexOutOfRangeException(
            $"Encountered too many elements in array of type {typeof(T)} with declared lengths {string.Join(", ", lengths)}.");

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for string fields. {field}");
        private static T ThrowLengthsFieldMissing() => throw new RequiredFieldMissingException("Serialized array is missing its lengths field.");

    }
}