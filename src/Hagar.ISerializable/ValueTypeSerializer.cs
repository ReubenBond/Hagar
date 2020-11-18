using Hagar.Buffers;
using Hagar.Codecs;
using System;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    /// <summary>
    /// Serializer for ISerializable value types.
    /// </summary>
    /// <typeparam name="T">The type which this serializer can serialize.</typeparam>
    internal class ValueTypeSerializer<T> : ISerializableSerializer where T : struct
    {
        public delegate void ValueConstructor(ref T value, SerializationInfo info, StreamingContext context);

        public delegate void SerializationCallback(ref T value, StreamingContext context);

        private static readonly Type Type = typeof(T);

        private readonly ValueConstructor _constructor;
        private readonly SerializationCallbacksFactory.SerializationCallbacks<SerializationCallback> _callbacks;

        private readonly IFormatterConverter _formatterConverter;
        private readonly StreamingContext _streamingContext;
        private readonly SerializationEntryCodec _entrySerializer;

        [SecurityCritical]
        public ValueTypeSerializer(
            ValueConstructor constructor,
            SerializationCallbacksFactory.SerializationCallbacks<SerializationCallback> callbacks,
            SerializationEntryCodec entrySerializer,
            StreamingContext streamingContext,
            IFormatterConverter formatterConverter)
        {
            _constructor = constructor;
            _callbacks = callbacks;
            _entrySerializer = entrySerializer;
            _streamingContext = streamingContext;
            _formatterConverter = formatterConverter;
        }

        [SecurityCritical]
        void ISerializableSerializer.WriteValue<TBufferWriter>(ref Writer<TBufferWriter> writer, object value)
        {
            var item = (T)value;
            _callbacks.OnSerializing?.Invoke(ref item, _streamingContext);

            var info = new SerializationInfo(Type, _formatterConverter);
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

            _callbacks.OnSerialized?.Invoke(ref item, _streamingContext);
        }

        [SecurityCritical]
        object ISerializableSerializer.ReadValue<TInput>(ref Reader<TInput> reader, Type type, uint placeholderReferenceId)
        {
            var info = new SerializationInfo(Type, _formatterConverter);
            T result = default;

            _callbacks.OnDeserializing?.Invoke(ref result, _streamingContext);

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

            _constructor(ref result, info, _streamingContext);
            _callbacks.OnDeserialized?.Invoke(ref result, _streamingContext);
            if (result is IDeserializationCallback callback)
            {
                callback.OnDeserialization(_streamingContext.Context);
            }

            return result;
        }
    }
}