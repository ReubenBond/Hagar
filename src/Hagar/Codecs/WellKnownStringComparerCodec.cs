using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Utilities;
using Hagar.WireProtocol;
using Microsoft.VisualBasic;
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
        private readonly StringComparer _invariantComparer;
        private readonly StringComparer _invariantIgnoreCaseComparer;
        private readonly StringComparer _currentCultureComparer;
        private readonly StringComparer _currentCultureIgnoreCaseComparer;

        private readonly Type _ordinalType;
        private readonly Type _ordinalIgnoreCaseType;
        private readonly Type _defaultEqualityType;
        private readonly Type _invariantType;
        private readonly Type _invariantIgnoreCaseType;
        private readonly Type _currentCultureType;
        private readonly Type _currentCultureIgnoreCaseType;

        public WellKnownStringComparerCodec()
        {
            _ordinalComparer = StringComparer.Ordinal;
            _ordinalIgnoreCaseComparer = StringComparer.OrdinalIgnoreCase;
            _defaultEqualityComparer = EqualityComparer<string>.Default;

            _invariantComparer = StringComparer.InvariantCulture;
            _invariantIgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
            _currentCultureComparer = StringComparer.CurrentCulture;
            _currentCultureIgnoreCaseComparer = StringComparer.CurrentCultureIgnoreCase;

            _ordinalType = _ordinalComparer.GetType();
            _ordinalIgnoreCaseType = _ordinalIgnoreCaseComparer.GetType();
            _defaultEqualityType = _defaultEqualityComparer.GetType();

            _invariantType = _invariantComparer.GetType();
            _invariantIgnoreCaseType = _invariantIgnoreCaseComparer.GetType();
            _currentCultureType = _currentCultureComparer.GetType();
            _currentCultureIgnoreCaseType = _currentCultureIgnoreCaseComparer.GetType();
        }

        public bool IsSupportedType(Type type) => CodecType == type
            || _defaultEqualityType == type
            || _ordinalType == type
            || _ordinalIgnoreCaseType == type
            || _invariantType == type
            || _invariantIgnoreCaseType == type
            || _currentCultureType == type
            || _currentCultureIgnoreCaseType == type;

        public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            ReferenceCodec.MarkValueField(reader.Session);
            var value = reader.ReadUInt32(field.WireType);

            switch (value)
            {
                case 0:
                    return null;
                case 1:
                    return _ordinalComparer;
                case 2:
                    return _ordinalIgnoreCaseComparer;
                case 3:
                    return _defaultEqualityComparer;
                case 4:
                    return _invariantComparer;
                case 5:
                    return _invariantIgnoreCaseComparer;
                case 6: 
                    return _currentCultureComparer;
                case 7:
                    return _currentCultureIgnoreCaseComparer;
                default:
                    ThrowNotSupported(field, value);
                    return null;
            }
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
            else if (_invariantComparer.Equals(value))
            {
                encoded = 4;
            }
            else if (_invariantIgnoreCaseComparer.Equals(value))
            {
                encoded = 5;
            }
            else if (_currentCultureComparer.Equals(value))
            {
                encoded = 6;
            }
            else if (_currentCultureIgnoreCaseComparer.Equals(value))
            {
                encoded = 7;
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