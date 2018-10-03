using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    internal class ValueTypeSerializerFactory
    {
        private readonly SerializationConstructorFactory constructorFactory;
        private readonly SerializationCallbacksFactory callbacksFactory;
        private readonly SerializationEntryCodec entrySerializer;
        private readonly StreamingContext streamingContext;
        private readonly IFormatterConverter formatterConverter;
        private readonly Func<Type, ISerializableSerializer> createSerializerDelegate;

        private readonly ConcurrentDictionary<Type, ISerializableSerializer> serializers =
            new ConcurrentDictionary<Type, ISerializableSerializer>();

        private readonly MethodInfo createTypedSerializerMethodInfo = typeof(ValueTypeSerializerFactory).GetMethod(
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
            this.constructorFactory = constructorFactory;
            this.callbacksFactory = callbacksFactory;
            this.entrySerializer = entrySerializer;
            this.streamingContext = streamingContext;
            this.formatterConverter = formatterConverter;
            this.createSerializerDelegate = type => (ISerializableSerializer) this.createTypedSerializerMethodInfo.MakeGenericMethod(type).Invoke(this, null);
        }

        [SecurityCritical]
        public ISerializableSerializer GetSerializer(Type type)
        {
            return this.serializers.GetOrAdd(type, this.createSerializerDelegate);
        }

        [SecurityCritical]
        private ISerializableSerializer CreateTypedSerializer<T>() where T : struct
        {
            var constructor = this.constructorFactory.GetSerializationConstructorDelegate<T, ValueTypeSerializer<T>.ValueConstructor>();
            var callbacks =
                this.callbacksFactory.GetValueTypeCallbacks<T, ValueTypeSerializer<T>.SerializationCallback>(typeof(T));
            var serializer = new ValueTypeSerializer<T>(constructor, callbacks, this.entrySerializer, this.streamingContext, this.formatterConverter);
            return serializer;
        }
    }
}