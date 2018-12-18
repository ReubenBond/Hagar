using System;
using System.Runtime.Serialization;

namespace Hagar
{
    internal static class ExceptionHelper
    {
        public static T ThrowArgumentOutOfRange<T>(string argument) => throw new ArgumentOutOfRangeException(argument);
        public static void ThrowArgumentOutOfRange(string argument) => throw new ArgumentOutOfRangeException(argument);
    }

    [Serializable]
    public class HagarException : Exception
    {
        public HagarException()
        {
        }

        public HagarException(string message) : base(message)
        {
        }

        public HagarException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HagarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class FieldIdNotPresentException : HagarException
    {
        public FieldIdNotPresentException() : base("Attempted to access the field id from a tag which cannot have a field id.")
        {
        }

        protected FieldIdNotPresentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class SchemaTypeInvalidException : HagarException
    {
        public SchemaTypeInvalidException() : base("Attempted to access the schema type from a tag which cannot have a schema type.")
        {
        }

        protected SchemaTypeInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class FieldTypeInvalidException : HagarException
    {
        public FieldTypeInvalidException() : base("Attempted to access the schema type from a tag which cannot have a schema type.")
        {
        }

        protected FieldTypeInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class FieldTypeMissingException : HagarException
    {
        public FieldTypeMissingException(Type type) : base($"Attempted to deserialize an instance of abstract type {type}. No concrete type was provided.")
        {
        }

        protected FieldTypeMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class ExtendedWireTypeInvalidException : HagarException
    {
        public ExtendedWireTypeInvalidException() : base(
            "Attempted to access the extended wire type from a tag which does not have an extended wire type.")
        {
        }

        protected ExtendedWireTypeInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UnsupportedWireTypeException : HagarException
    {
        public UnsupportedWireTypeException()
        {
        }

        public UnsupportedWireTypeException(string message) : base(message)
        {
        }

        protected UnsupportedWireTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class ReferenceNotFoundException : HagarException
    {
        public uint TargetReference { get; }
        public Type TargetReferenceType { get; }

        public ReferenceNotFoundException(Type targetType, uint targetId) : base(
            $"Reference with id {targetId} and type {targetType} not found.")
        {
            this.TargetReference = targetId;
            this.TargetReferenceType = targetType;
        }

        protected ReferenceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.TargetReference = info.GetUInt32(nameof(this.TargetReference));
            this.TargetReferenceType = (Type)info.GetValue(nameof(this.TargetReferenceType), typeof(Type));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.TargetReference), this.TargetReference);
            info.AddValue(nameof(this.TargetReferenceType), this.TargetReferenceType);
        }
    }

    [Serializable]
    public class UnknownReferencedTypeException : HagarException
    {
        public UnknownReferencedTypeException(uint reference) : base($"Unknown referenced type {reference}.")
        {
            this.Reference = reference;
        }

        protected UnknownReferencedTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            info.AddValue(nameof(this.Reference), this.Reference);
        }

        public uint Reference { get; set; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            this.Reference = info.GetUInt32(nameof(this.Reference));
        }
    }

    [Serializable]
    public class UnknownWellKnownTypeException : HagarException
    {
        public UnknownWellKnownTypeException(uint id) : base($"Unknown well-known type {id}.")
        {
            this.Id = id;
        }

        protected UnknownWellKnownTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            info.AddValue(nameof(this.Id), this.Id);
        }

        public uint Id { get; set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            this.Id = info.GetUInt32(nameof(this.Id));
        }
    }

    [Serializable]
    public class IllegalTypeException : HagarException
    {
        public IllegalTypeException(string typeName) : base($"Type \"{typeName}\" is not allowed.")
        {
            this.TypeName = typeName;
        }

        protected IllegalTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.TypeName = info.GetString(nameof(this.TypeName));
        }

        private string TypeName { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.TypeName), this.TypeName);
        }
    }

    [Serializable]
    public class TypeMissingException : HagarException
    {
        public TypeMissingException() : base("Expected a type but none were encountered.")
        {
        }

        protected TypeMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class RequiredFieldMissingException : HagarException
    {
        public RequiredFieldMissingException(string message) : base(message)
        {
        }

        protected RequiredFieldMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    [Serializable]
    public class CodecNotFoundException : HagarException
    {
        public CodecNotFoundException(string message) : base(message)
        {
        }

        protected CodecNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}