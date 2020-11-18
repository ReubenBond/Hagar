using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    [GenerateSerializer]
    internal struct SerializationEntrySurrogate
    {
        [Id(1)]
        public string Name { get; set; }

        [Id(2)]
        public object Value { get; set; }
    }
}