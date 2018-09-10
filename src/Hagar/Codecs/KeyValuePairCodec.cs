﻿using System;
using System.Collections.Generic;
using Hagar.Buffers;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public sealed class KeyValuePairCodec<TKey, TValue> : IFieldCodec<KeyValuePair<TKey, TValue>>
    {
        private readonly IFieldCodec<TKey> keyCodec;
        private readonly IFieldCodec<TValue> valueCodec;

        public KeyValuePairCodec(IFieldCodec<TKey> keyCodec, IFieldCodec<TValue> valueCodec)
        {
            this.keyCodec = HagarGeneratedCodeHelper.UnwrapService(this, keyCodec);
            this.valueCodec = HagarGeneratedCodeHelper.UnwrapService(this, valueCodec);
        }

        void IFieldCodec<KeyValuePair<TKey, TValue>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            KeyValuePair<TKey, TValue> value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            this.keyCodec.WriteField(ref writer, 0, typeof(TKey), value.Key);
            this.valueCodec.WriteField(ref writer, 1, typeof(TValue), value.Value);

            writer.WriteEndObject();
        }

        public KeyValuePair<TKey, TValue> ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            ReferenceCodec.MarkValueField(reader.Session);
            var key = default(TKey);
            var value = default(TValue);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        key = this.keyCodec.ReadValue(ref reader, header);
                        break;
                    case 1:
                        value = this.valueCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");
    }
}