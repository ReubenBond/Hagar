using System;
using System.Collections.Generic;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public sealed class DictionaryCodec<TKey, TValue> : IFieldCodec<Dictionary<TKey, TValue>>
    {
        private readonly IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec;
        private readonly IFieldCodec<IEqualityComparer<TKey>> comparerCodec;
        private readonly DictionaryActivator<TKey, TValue> activator;

        public DictionaryCodec(
            IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec,
            IFieldCodec<IEqualityComparer<TKey>> comparerCodec,
            DictionaryActivator<TKey, TValue> activator)
        {
            this.pairCodec = pairCodec;
            this.comparerCodec = comparerCodec;
            this.activator = activator;
        }

        void IFieldCodec<Dictionary<TKey, TValue>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Dictionary<TKey, TValue> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);
            
            if (value.Comparer != EqualityComparer<TKey>.Default)
            {
                this.comparerCodec.WriteField(ref writer, 0, typeof(IEqualityComparer<TKey>), value.Comparer);
            }

            var first = true;
            foreach (var element in value)
            {
                this.pairCodec.WriteField(ref writer, first ? 1U : 0, typeof(KeyValuePair<TKey, TValue>), element);
                first = false;
            }
            
            writer.WriteEndObject();
        }

        Dictionary<TKey, TValue> IFieldCodec<Dictionary<TKey, TValue>>.ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<Dictionary<TKey, TValue>>(ref reader, field);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            Dictionary<TKey, TValue> result = null;
            IEqualityComparer<TKey> comparer = null;
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        comparer = this.comparerCodec.ReadValue(ref reader, header);
                        break;
                    case 1:
                        if (result is null)
                        {
                            result = CreateInstance(comparer, reader.Session, placeholderReferenceId);
                        }

                        var pair = this.pairCodec.ReadValue(ref reader, header);
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
                result = this.CreateInstance(comparer, reader.Session, placeholderReferenceId);
            }

            return result;
        }

        private Dictionary<TKey, TValue> CreateInstance(IEqualityComparer<TKey> comparer, SerializerSession session, uint placeholderReferenceId)
        {
            var result = this.activator.Create(comparer);
            ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }
}
