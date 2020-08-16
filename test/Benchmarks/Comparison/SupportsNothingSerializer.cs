using Orleans.Serialization;
using System;

namespace Benchmarks.Comparison
{
    public class SupportsNothingSerializer : IExternalSerializer
    {
        public bool IsSupportedType(Type itemType) => false;

        public object DeepCopy(object source, ICopyContext context) => throw new NotSupportedException();

        public void Serialize(object item, ISerializationContext context, Type expectedType) => throw new NotSupportedException();

        public object Deserialize(Type expectedType, IDeserializationContext context) => throw new NotSupportedException();
    }
}