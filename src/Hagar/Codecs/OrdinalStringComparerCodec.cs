using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Utilities;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Hagar.Codecs
{
    public sealed class OrdinalStringComparerCodec : IGeneralizedCodec
    {
        private readonly Type _ordinalType;
        private readonly Type _ordinalIgnoreCaseType;
        private readonly FormatterConverter _formatterConverter;

        public OrdinalStringComparerCodec()
        {
            _ordinalType = StringComparer.Ordinal.GetType();
            _ordinalIgnoreCaseType = StringComparer.OrdinalIgnoreCase.GetType();
            _formatterConverter = new FormatterConverter();
        }

        public bool IsSupportedType(Type type) => _ordinalType == type || _ordinalIgnoreCaseType == type;

        public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            var value = reader.ReadUInt32(field.WireType);
            if (value == 0)
            {
                return null;
            }
            else if (value == 1)
            {
                return StringComparer.OrdinalIgnoreCase;
            }
            else
            {
                return StringComparer.Ordinal;
            }
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            if (value is null)
            {
                ReferenceCodec.WriteNullReference(ref writer, fieldIdDelta, expectedType);
                return;
            }

            var type = value.GetType();
            uint encoded = 0;
            if (value is ISerializable serializable)
            {
                var info = new SerializationInfo(type, _formatterConverter);
                serializable.GetObjectData(info, new StreamingContext(StreamingContextStates.All));
                var result = info.GetValue("_ignoreCase", typeof(bool));
                if (result is bool ignoreCase)
                {
                    encoded = ignoreCase ? 1U : 2U;
                }
            }

            if (encoded == 0)
            {
                if (type == _ordinalIgnoreCaseType)
                {
                    encoded = 1;
                }
                else if (type == _ordinalType)
                {
                    encoded = 2;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Unsupported type {type}");
                }
            }

            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, type, WireType.VarInt);
            writer.WriteVarUInt32(encoded);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for OrdinalComparer fields. {field}");
    }
}