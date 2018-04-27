using System;
using System.Collections.Concurrent;
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
#warning implement support for callbacks
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private readonly IFieldCodec<Type> typeCodec;
        private readonly IUntypedCodecProvider untypedCodecProvider;
        private readonly SerializationConstructorFactory constructorFactory = new SerializationConstructorFactory();
        private readonly SerializationCallbacksFactory serializationCallbacks = new SerializationCallbacksFactory();
        private readonly Func<Type, Action<object, SerializationInfo, StreamingContext>> createConstructorDelegate;
        
        private readonly ConcurrentDictionary<Type, Action<object, SerializationInfo, StreamingContext>> constructors =
            new ConcurrentDictionary<Type, Action<object, SerializationInfo, StreamingContext>>();
        
        // TODO: Should this be injectable?
        private static readonly IFormatterConverter FormatterConverter = new FormatterConverter();

        private readonly StreamingContext streamingContext = new StreamingContext();
        public static readonly Type CodecType = typeof(DotNetSerializableCodec);
        private readonly SerializationEntryCodec entrySerializer;

        public DotNetSerializableCodec(
            IFieldCodec<Type> typeCodec,
            IFieldCodec<string> stringCodec,
            IFieldCodec<object> objectCodec,
            IUntypedCodecProvider untypedCodecProvider)
        {
            this.typeCodec = typeCodec;
            this.untypedCodecProvider = untypedCodecProvider;
            this.entrySerializer = new SerializationEntryCodec(stringCodec, objectCodec);
            this.createConstructorDelegate = this.constructorFactory.GetSerializationConstructorDelegate;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            var serializableValue = (System.Runtime.Serialization.ISerializable) value;
            var type = value.GetType();
            var callbacks = this.serializationCallbacks.GetReferenceTypeCallbacks(type);
            var info = new SerializationInfo(type, FormatterConverter);
            callbacks.OnSerializing?.Invoke(value, streamingContext);
            serializableValue.GetObjectData(info, streamingContext);
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, CodecType, WireType.TagDelimited);
            this.typeCodec.WriteField(writer, session, 0, typeof(Type), type);
            var first = true;
            foreach (var field in info)
            {
                var surrogate = new SerializationEntrySurrogate(field);
                this.entrySerializer.WriteField(writer, session, first ? 1 : (uint)0, SerializationEntryCodec.SerializationEntryType, surrogate);
                if (first) first = false;
            }

            writer.WriteEndObject();
            callbacks.OnSerialized?.Invoke(value, streamingContext);
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference(reader, session, field, this.untypedCodecProvider, null);
            SerializationCallbacksFactory.SerializationCallbacks<Action<object, StreamingContext>> callbacks = null;
            object result = null;
            SerializationInfo info = null;
            uint fieldId = 0;

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        var type = this.typeCodec.ReadValue(reader, session, header);
                        info = new SerializationInfo(type, FormatterConverter);
                        result = FormatterServices.GetUninitializedObject(type);
                        callbacks = this.serializationCallbacks.GetReferenceTypeCallbacks(type);
                        ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
                        callbacks.OnDeserializing?.Invoke(result, streamingContext);
                        break;
                    case 1:
                        if (info == null) return ThrowTypeNotSpecified();

                        // Multiple entries may be read into the value.
                        var entry = this.entrySerializer.ReadValue(reader, session, header);
                        info.AddValue(entry.Name, entry.Value);
                        break;
                }
            }

            if (info == null) return ThrowTypeNotSpecified();
            
            var constructor = this.constructors.GetOrAdd(info.ObjectType, this.createConstructorDelegate);
            constructor(result, info, streamingContext);
            callbacks.OnDeserialized?.Invoke(result, streamingContext);
            return result;
        }

        public bool IsSupportedType(Type type) => type == CodecType || SerializableType.IsAssignableFrom(type);

        private static object ThrowTypeNotSpecified() => throw new InvalidOperationException(
            "The object type is required but was not present during deserialization.");
    }
}