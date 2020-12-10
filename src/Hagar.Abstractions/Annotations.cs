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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RegisterSerializerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RegisterActivatorAttribute : Attribute
    {
    }

/*
If object has a member with [ExtensionData], generate two serialization methods and one  deserialization method:
Serializer:
  If extension data field is not null, use extension serializer
  Else use regular serializer

Extension serializer:
  Between every member, check if there is extension data to serialize (this informs how the extension data should be structured)

    Deserializer:
Instead of 'consuming' unknown fields, copy them into buffers based upon the wire type:
  * Reference types need to point to the correct, deserialized object (or extension data buffer) so that they can be serialized again, pointing to the correct object.
  * Tagged types need to be recursively consumed
  * All other types can be copied into buffers directly, and added to referenced objects set for potential deserialization (just like 'consumed' fields)
 */
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public sealed class ExtensionDataAttribute : Attribute
    {
    }

    public interface IExtensibleData
    {
        [ExtensionData]
        object ExtensionData { get; set; }
    }
}