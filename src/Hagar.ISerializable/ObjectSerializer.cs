using Hagar.Buffers;
using Hagar.Codecs;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    /// <summary>
    /// Serializer for ISerializable reference types.
    /// </summary>
    internal sealed class ObjectSerializer : ISerializableSerializer
    {
        private readonly SerializationCallbacksFactory _serializationCallbacks;
        private readonly Func<Type, Action<object, SerializationInfo, StreamingContext>> _createConstructorDelegate;

        private readonly ConcurrentDictionary<Type, Action<object, SerializationInfo, StreamingContext>> _constructors =
            new();

        private readonly IFormatterConverter _formatterConverter;
        private readonly StreamingContext _streamingContext;
        private readonly SerializationEntryCodec _entrySerializer;

        public ObjectSerializer(
            SerializationEntryCodec entrySerializer,
            SerializationConstructorFactory constructorFactory,
            SerializationCallbacksFactory serializationCallbacks,
            IFormatterConverter formatterConverter,
            StreamingContext streamingContext)
        {
            _serializationCallbacks = serializationCallbacks;
            _formatterConverter = formatterConverter;
            _streamingContext = streamingContext;
            _entrySerializer = entrySerializer;
            _createConstructorDelegate = constructorFactory.GetSerializationConstructorDelegate;
        }

        [SecurityCritical]
        public void WriteValue<TBufferWriter>(ref Writer<TBufferWriter> writer, object value) where TBufferWriter : IBufferWriter<byte>
        {
            var type = value.GetType();
            var callbacks = _serializationCallbacks.GetReferenceTypeCallbacks(type);
            var info = new SerializationInfo(type, _formatterConverter);
            callbacks.OnSerializing?.Invoke(value, _streamingContext);
            ((System.Runtime.Serialization.ISerializable)value).GetObjectData(info, _streamingContext);

            var first = true;
            foreach (var field in info)
            {
                var surrogate = new SerializationEntrySurrogate
                {
                    Name = field.Name,
                    Value = field.Value
                };

                _entrySerializer.WriteField(ref writer, first ? 1 : (uint)0, SerializationEntryCodec.SerializationEntryType, surrogate);
                if (first)
                {
                    first = false;
                }
            }

            callbacks.OnSerialized?.Invoke(value, _streamingContext);
        }

        [SecurityCritical]
        public object ReadValue<TInput>(ref Reader<TInput> reader, Type type, uint placeholderReferenceId)
        {
            var callbacks = _serializationCallbacks.GetReferenceTypeCallbacks(type);

            var info = new SerializationInfo(type, _formatterConverter);
            var result = FormatterServices.GetUninitializedObject(type);

            ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
            callbacks.OnDeserializing?.Invoke(result, _streamingContext);

            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                if (fieldId == 1)
                {
                    var entry = _entrySerializer.ReadValue(ref reader, header);
                    info.AddValue(entry.Name, entry.Value);
                }
            }

            var constructor = _constructors.GetOrAdd(info.ObjectType, _createConstructorDelegate);
            constructor(result, info, _streamingContext);
            callbacks.OnDeserialized?.Invoke(result, _streamingContext);
            if (result is IDeserializationCallback callback)
            {
                callback.OnDeserialization(_streamingContext.Context);
            }

            return result;
        }
    }
}