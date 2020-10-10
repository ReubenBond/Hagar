using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.WireProtocol;
using System;

namespace Hagar.Codecs
{
    /// <summary>
    /// Serializer for multi-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    internal sealed class MultiDimensionalArrayCodec<T> : IGeneralizedCodec
    {
        private readonly IFieldCodec<int[]> _intArrayCodec;
        private readonly IFieldCodec<T> _elementCodec;

        public MultiDimensionalArrayCodec(IFieldCodec<int[]> intArrayCodec, IFieldCodec<T> elementCodec)
        {
            _intArrayCodec = HagarGeneratedCodeHelper.UnwrapService(this, intArrayCodec);
            _elementCodec = HagarGeneratedCodeHelper.UnwrapService(this, elementCodec);
        }

        void IFieldCodec<object>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            var array = (Array)value;
            var rank = array.Rank;

            var lengths = new int[rank];
            var indices = new int[rank];

            // Write array lengths.
            for (var i = 0; i < rank; i++)
            {
                lengths[i] = array.GetLength(i);
            }

            _intArrayCodec.WriteField(ref writer, 0, typeof(int[]), lengths);

            var remaining = array.Length;
            var first = true;
            while (remaining-- > 0)
            {
                var element = array.GetValue(indices);
                _elementCodec.WriteField(ref writer, first ? 1U : 0, typeof(T), (T)element);
                first = false;

                // Increment the indices array by 1.
                if (remaining > 0)
                {
                    var idx = rank - 1;
                    while (idx >= 0 && ++indices[idx] >= lengths[idx])
                    {
                        indices[idx] = 0;
                        --idx;
                        if (idx < 0)
                        {
                            _ = ThrowIndexOutOfRangeException(lengths);
                        }
                    }
                }
            }


            writer.WriteEndObject();
        }

        object IFieldCodec<object>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
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
            Array result = null;
            uint fieldId = 0;
            int[] lengths = null;
            int[] indices = null;
            var rank = 0;
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
                        {
                            lengths = _intArrayCodec.ReadValue(ref reader, header);
                            rank = lengths.Length;

                            // Multi-dimensional arrays must be indexed using indexing arrays, so create one now.
                            indices = new int[rank];
                            result = Array.CreateInstance(typeof(T), lengths);
                            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
                            break;
                        }
                    case 1:
                        {
                            if (result is null)
                            {
                                return ThrowLengthsFieldMissing();
                            }

                            var element = _elementCodec.ReadValue(ref reader, header);
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
                        reader.ConsumeUnknownField(header);
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