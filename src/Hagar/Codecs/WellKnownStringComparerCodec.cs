using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Utilities;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hagar.Codecs
{
    [WellKnownAlias("StringComparer")]
    public sealed class WellKnownStringComparerCodec : IGeneralizedCodec
    {
        private static readonly Type CodecType = typeof(WellKnownStringComparerCodec);
        private readonly StringComparer _ordinalComparer;
        private readonly StringComparer _ordinalIgnoreCaseComparer;
        private readonly EqualityComparer<string> _defaultEqualityComparer;
        private readonly Type _ordinalType;
        private readonly Type _ordinalIgnoreCaseType;
        private readonly Type _defaultEqualityType;

        public WellKnownStringComparerCodec()
        {
            _ordinalComparer = StringComparer.Ordinal;
            _ordinalIgnoreCaseComparer = StringComparer.OrdinalIgnoreCase;
            _defaultEqualityComparer = EqualityComparer<string>.Default;

            _ordinalType = _ordinalComparer.GetType();
            _ordinalIgnoreCaseType = _ordinalIgnoreCaseComparer.GetType();
            _defaultEqualityType = _defaultEqualityComparer.GetType();
        }

        public bool IsSupportedType(Type type) => CodecType == type || _defaultEqualityType == type || _ordinalType == type || _ordinalIgnoreCaseType == type;

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
                return _ordinalComparer;
            }
            else if (value == 2)
            {
                return _ordinalIgnoreCaseComparer;
            }
            else if (value == 3)
            {
                return _defaultEqualityComparer;
            }

            ThrowNotSupported(field, value);
            return null;
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            uint encoded;
            if (value is null)
            {
                encoded = 0;
            }
            else if (_ordinalComparer.Equals(value))
            {
                encoded = 1;
            }
            else if (_ordinalIgnoreCaseComparer.Equals(value))
            {
                encoded = 2;
            }
            else if (_defaultEqualityComparer.Equals(value))
            {
                encoded = 3;
            }
            else
            {
                ThrowNotSupported(value.GetType());
                return;
            }

            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(WellKnownStringComparerCodec), WireType.VarInt);
            writer.WriteVarUInt32(encoded);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for OrdinalComparer fields. {field}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupported(Field field, uint value) => throw new NotSupportedException($"Values of type {field.FieldType} are not supported. Value: {value}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupported(Type type) => throw new NotSupportedException($"Values of type {type} are not supported");
    }
}