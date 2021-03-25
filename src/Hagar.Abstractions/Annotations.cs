using System;

namespace Hagar
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class GenerateSerializerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class GenerateMethodSerializersAttribute : Attribute
    {
        public GenerateMethodSerializersAttribute(Type proxyBase, bool isExtension = false)
        {
            ProxyBase = proxyBase;
            IsExtension = isExtension;
        }

        public Type ProxyBase { get; }
        public bool IsExtension { get; }
        public Type ValueTaskInvoker { get; init; }
        public Type ValueTask1Invoker { get; init; }
        public Type TaskInvoker { get; init; }
        public Type Task1Invoker { get; init; }
        public Type VoidInvoker { get; init; }
    }

    /// <summary>
    /// Applied to method attributes on invokable interfaces to specify the name of the method to call when submitting a request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SubmitInvokableMethodNameAttribute : Attribute
    {
        public SubmitInvokableMethodNameAttribute(string invokeMethodName)
        {
            InvokeMethodName = invokeMethodName;
        }

        public string InvokeMethodName { get; }
    }

    /// <summary>
    /// Applied to method attributes on invokable interfaces to specify the name of the method to call when submitting a request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class InvokablePropertyValueAttribute : Attribute
    {
        public InvokablePropertyValueAttribute(string propertyName, object propertyValue)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
            AttributeArgumentIndex = -1;
        }

        public InvokablePropertyValueAttribute(string propertyName)
        {
            PropertyName = propertyName;
            PropertyValue = null;
            AttributeArgumentIndex = 0;
        }

        public string PropertyName { get; }
        public int AttributeArgumentIndex { get; init; }
        public object PropertyValue { get; }
    }

    /// <summary>
    /// Applied to proxy base types and to attribute types used on invokable interface methods to specify the base type for the <see cref="IInvokable"/> which represents a method call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DefaultInvokableBaseTypeAttribute : Attribute
    {
        public DefaultInvokableBaseTypeAttribute(Type returnType, Type invokableBaseType) { }
        public Type ProxyBaseClass { get; }
        public Type ReturnType { get; }
        public Type InvokableBaseType { get; }
    }

    /// <summary>
    /// Applied to attribute types used on invokable interface methods to specify the base type for the <see cref="IInvokable"/> which represents a method call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class InvokableBaseTypeAttribute : Attribute
    {
        public InvokableBaseTypeAttribute(Type proxyBaseClass, Type returnType, Type invokableBaseType) { }
        public Type ProxyBaseClass { get; }
        public Type ReturnType { get; }
        public Type InvokableBaseType { get; }
    }

    /// <summary>
    /// Applied to method attributes on invokable interfaces to specify the name of the method to call to get a completion source which is submitted to the submit method and eventually returned to the caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GetCompletionSourceMethodNameAttribute : Attribute
    {
        public GetCompletionSourceMethodNameAttribute(string methodName)
        {
            MethodName = methodName;
        }

        public string MethodName { get; }
    }

    /// <summary>
    /// Applied to method attributes on invokable interfaces to specify the name of the method to call to adapt the completion source into the method return type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AdaptCompletionSourceMethodNameAttribute : Attribute
    {
        public AdaptCompletionSourceMethodNameAttribute(Type returnType, string methodName)
        {
            ReturnType = returnType;
            MethodName = methodName;
        }

        public Type ReturnType { get; }
        public string MethodName { get; }
    }

    [AttributeUsage(
        AttributeTargets.Field
        | AttributeTargets.Property
        | AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Enum
        | AttributeTargets.Method)]
    public sealed class IdAttribute : Attribute
    {
        public IdAttribute(ushort id)
        {
            Id = id;
        }

        public ushort Id { get; }
    }

    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Enum
        | AttributeTargets.Method)]
    public sealed class WellKnownIdAttribute : Attribute
    {
        public WellKnownIdAttribute(uint id)
        {
            Id = id;
        }

        public uint Id { get; }
    }

    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Enum
        | AttributeTargets.Method)]
    public sealed class WellKnownAliasAttribute : Attribute
    {
        public WellKnownAliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RegisterSerializerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RegisterActivatorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RegisterCopierAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class UseActivatorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class SuppressReferenceTrackingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class OmitDefaultMemberValuesAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ImmutableAttribute : Attribute
    {
    }

    [Immutable]
    public readonly struct Immutable<T>
    {
        public T Value { get; init; }
    }

    /// <summary>
    /// Specifies an assembly to be added as an application part.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ApplicationPartAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationPartAttribute" />.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public ApplicationPartAttribute(string assemblyName)
        {
            AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        }

        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public string AssemblyName { get; }
    }
}