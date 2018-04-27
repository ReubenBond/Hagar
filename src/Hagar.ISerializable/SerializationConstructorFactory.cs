using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;

[assembly: SecurityTransparent]
namespace Hagar.ISerializable
{
    /// <summary>
    /// Creates delegates for calling ISerializable-conformant constructors.
    /// </summary>
    internal class SerializationConstructorFactory
    {
        private readonly Func<Type, object> createConstructorDelegate;

        private readonly ConcurrentDictionary<Type, object> constructors = new ConcurrentDictionary<Type, object>();

        private readonly Type[] serializationConstructorParameterTypes = { typeof(SerializationInfo), typeof(StreamingContext) };

        public SerializationConstructorFactory()
        {
            this.createConstructorDelegate = this
                .GetSerializationConstructorInvoker<object, Action<object, SerializationInfo, StreamingContext>>;
        }

        public Action<object, SerializationInfo, StreamingContext> GetSerializationConstructorDelegate(Type type)
        {
            return (Action<object, SerializationInfo, StreamingContext>)this.constructors.GetOrAdd(
                type,
                this.createConstructorDelegate);
        }

        public TConstructor GetSerializationConstructorDelegate<TOwner, TConstructor>()
        {
            return (TConstructor)this.constructors.GetOrAdd(
                typeof(TOwner),
                type => (object)this.GetSerializationConstructorInvoker<TOwner, TConstructor>(type));
        }

        public ConstructorInfo GetSerializationConstructor(Type type)
        {
            return type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                this.serializationConstructorParameterTypes,
                null);
        }

        private TConstructor GetSerializationConstructorInvoker<TOwner, TConstructor>(Type type)
        {
            var constructor = this.GetSerializationConstructor(type);
            if (constructor == null) throw new SerializationException($"{nameof(ISerializable)} constructor not found on type {type}.");

            Type[] parameterTypes;
            if (typeof(TOwner).IsValueType) parameterTypes = new[] {typeof(TOwner).MakeByRefType(), typeof(SerializationInfo), typeof(StreamingContext)};
            else parameterTypes = new[] { typeof(object), typeof(SerializationInfo), typeof(StreamingContext) };

            var method = new DynamicMethod($"{type}_serialization_ctor", null, parameterTypes, typeof(TOwner), skipVisibility: true);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            if (type != typeof(TOwner)) il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, constructor);
            il.Emit(OpCodes.Ret);

            object result = method.CreateDelegate(typeof(TConstructor));
            return (TConstructor)result;
        }
    }
}