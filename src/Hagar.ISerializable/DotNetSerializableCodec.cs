using System;
using System.Reflection;
using System.Runtime.Serialization;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    public class DotNetSerializableCodec : IGeneralizedCodec
    {
        public static readonly Type CodecType = typeof(DotNetSerializableCodec);
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private readonly IFieldCodec<Type> typeCodec;
        private readonly IUntypedCodecProvider untypedCodecProvider;
        
        private readonly StreamingContext streamingContext = new StreamingContext();
        private readonly ObjectSerializer objectSerializer;
        private readonly ValueTypeSerializerFactory valueTypeSerializerFactory;

        public DotNetSerializableCodec(
            IFieldCodec<Type> typeCodec,
            IFieldCodec<string> stringCodec,
            IFieldCodec<object> objectCodec,
            IUntypedCodecProvider untypedCodecProvider)
        {
            this.typeCodec = typeCodec;
            this.untypedCodecProvider = untypedCodecProvider;
            var entrySerializer = new SerializationEntryCodec(stringCodec, objectCodec);
            var constructorFactory = new SerializationConstructorFactory();
            var serializationCallbacks = new SerializationCallbacksFactory();
            var formatterConverter = new FormatterConverter();

            this.objectSerializer = new ObjectSerializer(
                entrySerializer,
                untypedCodecProvider,
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

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            var type = value.GetType();
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, CodecType, WireType.TagDelimited);
            this.typeCodec.WriteField(writer, session, 0, typeof(Type), type);

            if (type.IsValueType)
            {
                var serializer = this.valueTypeSerializerFactory.GetSerializer(type);
                serializer.WriteValue(writer, session, value);
            }
            else
            {
                this.objectSerializer.WriteValue(writer, session, value);
            }

            writer.WriteEndObject();
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference(reader, session, field, this.untypedCodecProvider, null);
          
            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            var header = reader.ReadFieldHeader(session);
                
            var type = this.typeCodec.ReadValue(reader, session, header);

            if (type.IsValueType)
            {
                var serializer = this.valueTypeSerializerFactory.GetSerializer(type);
                return serializer.ReadValue(reader, session, type, placeholderReferenceId);
            }

            return this.objectSerializer.ReadValue(reader, session, type, placeholderReferenceId);
        }

        public bool IsSupportedType(Type type) =>
            type == CodecType || SerializableType.IsAssignableFrom(type) && SerializationConstructorFactory.HasSerializationConstructor(type);
    }
}