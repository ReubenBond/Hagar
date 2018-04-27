using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;

[assembly: SecurityTransparent]
namespace Hagar.ISerializable
{
    internal class SerializationConstructorFactory
    {
        private readonly Type delegateType = typeof(Action<object, SerializationInfo, StreamingContext>);
        private readonly Type[] parameterTypes = {typeof(object), typeof(SerializationInfo), typeof(StreamingContext)};

        public Action<object, SerializationInfo, StreamingContext> GetSerializationConstructorInvoker(Type type)
        {
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] {typeof(SerializationInfo), typeof(StreamingContext)},
                null);
            if (constructor == null) return ThrowSerializationConstructorNotFound(type);
            
            var method = new DynamicMethod($"{type}_serialization_ctor", null, this.parameterTypes, this.delegateType, skipVisibility: true);
            var il = method.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, constructor);
            il.Emit(OpCodes.Ret);

            return (Action<object, SerializationInfo, StreamingContext>) method.CreateDelegate(this.delegateType);
        }

        private static Action<object, SerializationInfo, StreamingContext> ThrowSerializationConstructorNotFound(Type type) =>
            throw new SerializationConstructorNotFoundException(type);
    }
}