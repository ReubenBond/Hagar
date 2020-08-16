using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    internal struct SerializationEntrySurrogate
    {
        [SecurityCritical]
        public SerializationEntrySurrogate(SerializationEntry entry)
        {
            Name = entry.Name;
            Value = entry.Value;
        }

        public object Value { get; set; }
        public string Name { get; set; }
    }
}