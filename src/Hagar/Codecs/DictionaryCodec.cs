using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    [RegisterSerializer]
    public sealed class DictionaryCodec<TKey, TValue> : IFieldCodec<Dictionary<TKey, TValue>>
    {
        private static readonly Type CodecFieldType = typeof(KeyValuePair<TKey, TValue>);

        private readonly IFieldCodec<KeyValuePair<TKey, TValue>> _pairCodec;
        private readonly IFieldCodec<IEqualityComparer<TKey>> _comparerCodec;
        private readonly DictionaryActivator<TKey, TValue> _activator;

        public DictionaryCodec(
            IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec,
            IFieldCodec<IEqualityComparer<TKey>> comparerCodec,
            DictionaryActivator<TKey, TValue> activator)
        {
            _pairCodec = pairCodec;
            _comparerCodec = comparerCodec;
            _activator = activator;
        }

        void IFieldCodec<Dictionary<TKey, TValue>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Dictionary<TKey, TValue> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            if (value.Comparer != EqualityComparer<TKey>.Default)
            {
                _comparerCodec.WriteField(ref writer, 0, typeof(IEqualityComparer<TKey>), value.Comparer);
            }

            uint innerFieldIdDelta = 1;
            foreach (var element in value)
            {
                _pairCodec.WriteField(ref writer, innerFieldIdDelta, CodecFieldType, element);
                innerFieldIdDelta = 0;
            }

            writer.WriteEndObject();
        }

        Dictionary<TKey, TValue> IFieldCodec<Dictionary<TKey, TValue>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<Dictionary<TKey, TValue>, TInput>(ref reader, field);
            }

            if (field.WireType != WireType.TagDelimited)
            {
                ThrowUnsupportedWireTypeException(field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            Dictionary<TKey, TValue> result = null;
            IEqualityComparer<TKey> comparer = null;
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
                        comparer = _comparerCodec.ReadValue(ref reader, header);
                        break;
                    case 1:
                        if (result is null)
                        {
                            result = CreateInstance(comparer, reader.Session, placeholderReferenceId);
                        }

                        var pair = _pairCodec.ReadValue(ref reader, header);
                        // ReSharper disable once PossibleNullReferenceException
                        result.Add(pair.Key, pair.Value);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            if (result is null)
            {
                result = CreateInstance(comparer, reader.Session, placeholderReferenceId);
            }

            return result;
        }

        private Dictionary<TKey, TValue> CreateInstance(IEqualityComparer<TKey> comparer, SerializerSession session, uint placeholderReferenceId)
        {
            var result = _activator.Create(comparer);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }
}