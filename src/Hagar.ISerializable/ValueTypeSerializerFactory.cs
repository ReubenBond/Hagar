using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    internal class ValueTypeSerializerFactory
    {
        private readonly SerializationConstructorFactory _constructorFactory;
        private readonly SerializationCallbacksFactory _callbacksFactory;
        private readonly SerializationEntryCodec _entrySerializer;
        private readonly StreamingContext _streamingContext;
        private readonly IFormatterConverter _formatterConverter;
        private readonly Func<Type, ISerializableSerializer> _createSerializerDelegate;

        private readonly ConcurrentDictionary<Type, ISerializableSerializer> _serializers =
            new ConcurrentDictionary<Type, ISerializableSerializer>();

        private readonly MethodInfo _createTypedSerializerMethodInfo = typeof(ValueTypeSerializerFactory).GetMethod(
            nameof(CreateTypedSerializer),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [SecurityCritical]
        public ValueTypeSerializerFactory(
            SerializationEntryCodec entrySerializer,
            SerializationConstructorFactory constructorFactory,
            SerializationCallbacksFactory callbacksFactory,
            IFormatterConverter formatterConverter,
            StreamingContext streamingContext)
        {
            _constructorFactory = constructorFactory;
            _callbacksFactory = callbacksFactory;
            _entrySerializer = entrySerializer;
            _streamingContext = streamingContext;
            _formatterConverter = formatterConverter;
            _createSerializerDelegate = type => (ISerializableSerializer)_createTypedSerializerMethodInfo.MakeGenericMethod(type).Invoke(this, null);
        }

        [SecurityCritical]
        public ISerializableSerializer GetSerializer(Type type) => _serializers.GetOrAdd(type, _createSerializerDelegate);

        [SecurityCritical]
        private ISerializableSerializer CreateTypedSerializer<T>() where T : struct
        {
            var constructor = _constructorFactory.GetSerializationConstructorDelegate<T, ValueTypeSerializer<T>.ValueConstructor>();
            var callbacks =
                _callbacksFactory.GetValueTypeCallbacks<T, ValueTypeSerializer<T>.SerializationCallback>(typeof(T));
            var serializer = new ValueTypeSerializer<T>(constructor, callbacks, _entrySerializer, _streamingContext, _formatterConverter);
            return serializer;
        }
    }
}