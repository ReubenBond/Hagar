using System;
using System.Collections.Generic;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class KeyValuePairCodec<TKey, TValue> : IFieldCodec<KeyValuePair<TKey, TValue>>
    {
        private readonly IFieldCodec<TKey> keyCodec;
        private readonly IFieldCodec<TValue> valueCodec;

        public KeyValuePairCodec(IFieldCodec<TKey> keyCodec, IFieldCodec<TValue> valueCodec)
        {
            this.keyCodec = keyCodec;
            this.valueCodec = valueCodec;
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, KeyValuePair<TKey, TValue> value)
        {
            ReferenceCodec.MarkValueField(session);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.keyCodec.WriteField(ref writer, session, 0, typeof(TKey), value.Key);
            this.valueCodec.WriteField(ref writer, session, 1, typeof(TValue), value.Value);

            
            writer.WriteEndObject();
        }

        public KeyValuePair<TKey, TValue> ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            ReferenceCodec.MarkValueField(session);
            var key = default(TKey);
            var value = default(TValue);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        key = this.keyCodec.ReadValue(ref reader, session, header);
                        break;
                    case 1:
                        value = this.valueCodec.ReadValue(ref reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }
}