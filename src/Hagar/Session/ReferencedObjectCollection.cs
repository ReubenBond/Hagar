using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hagar.Session
{
    public sealed class ReferencedObjectCollection
    {
        private readonly Dictionary<uint, object> references = new Dictionary<uint, object>();
        private readonly Dictionary<object, uint> referenceToIdMap = new Dictionary<object, uint>(ReferenceEqualsComparer.Instance);

        public bool TryGetReferencedObject(uint reference, out object value)
        {
            // Reference 0 is always null.
            if (reference == 0)
            {
                value = null;
                return true;
            }

            return this.references.TryGetValue(reference, out value);
        }

        public void MarkValueField() => ++this.CurrentReferenceId;

        public bool GetOrAddReference(object value, out uint reference)
        {
            // Null is always at reference 0
            if (value == null)
            {
                reference = 0;
                return true;
            }

            if (this.referenceToIdMap.TryGetValue(value, out reference)) return true;
            
            // Add the reference.
            reference = ++this.CurrentReferenceId;
            this.referenceToIdMap.Add(value, this.CurrentReferenceId);
            return false;
        }

        public void RecordReferenceField(object value) => RecordReferenceField(value, ++this.CurrentReferenceId);

        public void RecordReferenceField(object value, uint referenceId)
        {
            if (value == null) return;
            this.references[referenceId] = value;
        }

        public Dictionary<uint, object> CopyReferenceTable() => new Dictionary<uint, object>(this.references);
        public Dictionary<object, uint> CopyIdTable() => new Dictionary<object, uint>(this.referenceToIdMap);

        public uint CurrentReferenceId { get; set; }

        public void Reset()
        {
            this.references.Clear();
            this.referenceToIdMap.Clear();
            this.CurrentReferenceId = 0;
        }

        internal class ReferenceEqualsComparer : IEqualityComparer<object>
        {
            /// <summary>
            /// Gets an instance of this class.
            /// </summary>
            public static ReferenceEqualsComparer Instance { get; } = new ReferenceEqualsComparer();

            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}