using System;
using System.Collections.Generic;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class DictionaryCodec<TKey, TValue> : IFieldCodec<Dictionary<TKey, TValue>>
    {
        private readonly IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec;
        private readonly IUntypedCodecProvider codecProvider;
        private readonly IFieldCodec<IEqualityComparer<TKey>> comparerCodec;
        private readonly DictionaryActivator<TKey, TValue> activator;

        public DictionaryCodec(
            IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec,
            IUntypedCodecProvider codecProvider,
            IFieldCodec<IEqualityComparer<TKey>> comparerCodec,
            DictionaryActivator<TKey, TValue> activator)
        {
            this.pairCodec = pairCodec;
            this.codecProvider = codecProvider;
            this.comparerCodec = comparerCodec;
            this.activator = activator;
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Dictionary<TKey, TValue> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);
            
            if (value.Comparer != EqualityComparer<TKey>.Default)
            {
                this.comparerCodec.WriteField(ref writer, session, 0, typeof(IEqualityComparer<TKey>), value.Comparer);
            }

            var first = true;
            foreach (var element in value)
            {
                this.pairCodec.WriteField(ref writer, session, first ? 1U : 0, typeof(KeyValuePair<TKey, TValue>), element);
                first = false;
            }
            
            writer.WriteEndObject();
        }

        public Dictionary<TKey, TValue> ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<Dictionary<TKey, TValue>>(ref reader, session, field);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            Dictionary<TKey, TValue> result = null;
            IEqualityComparer<TKey> comparer = null;
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        comparer = this.comparerCodec.ReadValue(ref reader, session, header);
                        break;
                    case 1:
                        if (result == null)
                        {
                            result = CreateInstance(comparer, session, placeholderReferenceId);
                        }

                        var pair = this.pairCodec.ReadValue(ref reader, session, header);
                        // ReSharper disable once PossibleNullReferenceException
                        result.Add(pair.Key, pair.Value);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            if (result == null)
            {
                result = CreateInstance(comparer, session, placeholderReferenceId);
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
