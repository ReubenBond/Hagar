using System;
using System.Runtime.Serialization;

namespace Hagar.ISerializable
{
    [Serializable]
    public class SerializationConstructorNotFoundException : Exception
    {
        public SerializationConstructorNotFoundException(Type type) : base(
            (string) $"Could not find a suitable serialization constructor on type {type.FullName}")
        {
        }

        protected SerializationConstructorNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}