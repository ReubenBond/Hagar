using System.Runtime.Serialization;

namespace Hagar.ISerializable
{
    internal struct SerializationEntrySurrogate
    {
        public SerializationEntrySurrogate(SerializationEntry entry)
        {
            this.Name = entry.Name;
            this.Value = entry.Value;
        }

        public object Value { get; set; }
        public string Name { get; set; }
    }
}