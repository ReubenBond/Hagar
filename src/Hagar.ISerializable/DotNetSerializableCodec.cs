using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    public class DotNetSerializableCodec : IGeneralizedCodec
    {
        public static readonly Type CodecType = typeof(DotNetSerializableCodec);
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private readonly IFieldCodec<Type> _typeCodec;

        private readonly StreamingContext _streamingContext;
        private readonly ObjectSerializer _objectSerializer;
        private readonly ValueTypeSerializerFactory _valueTypeSerializerFactory;

        public DotNetSerializableCodec(
            IFieldCodec<Type> typeCodec)
        {
            _streamingContext = default;
            _typeCodec = typeCodec;
            var entrySerializer = new SerializationEntryCodec();
            var constructorFactory = new SerializationConstructorFactory();
            var serializationCallbacks = new SerializationCallbacksFactory();
            var formatterConverter = new FormatterConverter();

            _objectSerializer = new ObjectSerializer(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                _streamingContext);

            _valueTypeSerializerFactory = new ValueTypeSerializerFactory(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                _streamingContext);
        }

        [SecurityCritical]
        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            var type = value.GetType();
            writer.WriteFieldHeader(fieldIdDelta, expectedType, CodecType, WireType.TagDelimited);
            _typeCodec.WriteField(ref writer, 0, typeof(Type), type);

            if (type.IsValueType)
            {
                var serializer = _valueTypeSerializerFactory.GetSerializer(type);
                serializer.WriteValue(ref writer, value);
            }
            else
            {
                _objectSerializer.WriteValue(ref writer, value);
            }

            writer.WriteEndObject();
        }

        [SecurityCritical]
        public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            var header = reader.ReadFieldHeader();

            var type = _typeCodec.ReadValue(ref reader, header);

            if (type.IsValueType)
            {
                var serializer = _valueTypeSerializerFactory.GetSerializer(type);
                return serializer.ReadValue(ref reader, type, placeholderReferenceId);
            }

            return _objectSerializer.ReadValue(ref reader, type, placeholderReferenceId);
        }

        [SecurityCritical]
        public bool IsSupportedType(Type type) =>
            type == CodecType || SerializableType.IsAssignableFrom(type) && SerializationConstructorFactory.HasSerializationConstructor(type);
    }
}