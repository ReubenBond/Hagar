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
    public sealed class UseActivatorAttribute : Attribute
    {
    }
}