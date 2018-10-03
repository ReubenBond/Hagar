using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.Serialization;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    public class DotNetSerializableCodec : IGeneralizedCodec
    {
        public static readonly Type CodecType = typeof(DotNetSerializableCodec);
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private readonly IFieldCodec<Type> typeCodec;
        
        private readonly StreamingContext streamingContext = new StreamingContext();
        private readonly ObjectSerializer objectSerializer;
        private readonly ValueTypeSerializerFactory valueTypeSerializerFactory;

        public DotNetSerializableCodec(
            IFieldCodec<Type> typeCodec)
        {
            this.typeCodec = typeCodec;
            var entrySerializer = new SerializationEntryCodec();
            var constructorFactory = new SerializationConstructorFactory();
            var serializationCallbacks = new SerializationCallbacksFactory();
            var formatterConverter = new FormatterConverter();

            this.objectSerializer = new ObjectSerializer(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                this.streamingContext);

            this.valueTypeSerializerFactory = new ValueTypeSerializerFactory(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                this.streamingContext);
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) return;
            var type = value.GetType();
            writer.WriteFieldHeader(fieldIdDelta, expectedType, CodecType, WireType.TagDelimited);
            this.typeCodec.WriteField(ref writer, 0, typeof(Type), type);

            if (type.IsValueType)
            {
                var serializer = this.valueTypeSerializerFactory.GetSerializer(type);
                serializer.WriteValue(ref writer, value);
            }
            else
            {
                this.objectSerializer.WriteValue(ref writer, value);
            }
            
            writer.WriteEndObject();
        }

        public object ReadValue(ref Reader reader, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<object>(ref reader, field);
          
            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
            var header = reader.ReadFieldHeader();
                
            var type = this.typeCodec.ReadValue(ref reader, header);

            if (type.IsValueType)
            {
                var serializer = this.valueTypeSerializerFactory.GetSerializer(type);
                return serializer.ReadValue(ref reader, type, placeholderReferenceId);
            }

            return this.objectSerializer.ReadValue(ref reader, type, placeholderReferenceId);
        }

        public bool IsSupportedType(Type type) =>
            type == CodecType || SerializableType.IsAssignableFrom(type) && SerializationConstructorFactory.HasSerializationConstructor(type);
    }
}