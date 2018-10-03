using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    internal struct SerializationEntrySurrogate
    {
        [SecurityCritical]
        public SerializationEntrySurrogate(SerializationEntry entry)
        {
            this.Name = entry.Name;
            this.Value = entry.Value;
        }

        public object Value { get; set; }
        public string Name { get; set; }
    }
}