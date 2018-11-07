using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Hagar.Session
{
    public sealed class ReferencedObjectCollection
    {
        private readonly struct ReferencePair
        {
            public ReferencePair(uint id, object @object)
            {
                this.Id = id;
                this.Object = @object;
            }

            public uint Id { get; }

            public object Object { get; }
        }

        private int referenceToObjectCount;
        private ReferencePair[] referenceToObject = new ReferencePair[64];

        private int objectToReferenceCount;
        private ReferencePair[] objectToReference = new ReferencePair[64];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReferencedObject(uint reference, out object value)
        {
            // Reference 0 is always null.
            if (reference == 0)
            {
                value = null;
                return true;
            }

            // TODO: Binary search
            for (int i = 0; i < this.referenceToObjectCount; ++i)
            {
                if (this.referenceToObject[i].Id == reference)
                {
                    value = this.referenceToObject[i].Object;
                    return true;
                }
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkValueField() => ++this.CurrentReferenceId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetOrAddReference(object value, out uint reference)
        {
            // Null is always at reference 0
            if (value == null)
            {
                reference = 0;
                return true;
            }

            // TODO: Binary search
            for (int i = 0; i < this.objectToReferenceCount; ++i)
            {
                if (ReferenceEquals(this.objectToReference[i].Object, value))
                {
                    reference = this.objectToReference[i].Id;
                    return true;
                }
            }

            // Add the reference.
            reference = ++this.CurrentReferenceId;
            AddToReferenceToIdMap(value, reference);
            return false;
        }

        private void AddToReferenceToIdMap(object value, uint reference)
        {
            if (this.objectToReferenceCount >= this.objectToReference.Length)
            {
                var old = this.objectToReference;
                this.objectToReference = new ReferencePair[this.objectToReference.Length * 2];
                Array.Copy(old, this.objectToReference, this.objectToReferenceCount);
            }

            this.objectToReference[this.objectToReferenceCount++] = new ReferencePair(reference, value);
        }

        private void AddToReferences(object value, uint reference)
        {
            if (this.referenceToObjectCount >= this.referenceToObject.Length)
            {
                var old = this.referenceToObject;
                this.referenceToObject = new ReferencePair[this.referenceToObject.Length * 2];
                Array.Copy(old, this.referenceToObject, this.referenceToObjectCount);
            }

            this.referenceToObject[this.referenceToObjectCount++] = new ReferencePair(reference, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordReferenceField(object value) => RecordReferenceField(value, ++this.CurrentReferenceId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordReferenceField(object value, uint referenceId)
        {
            if (value == null) return;
            AddToReferences(value, referenceId);
        }

        public Dictionary<uint, object> CopyReferenceTable() => this.referenceToObject.Take(this.referenceToObjectCount).ToDictionary(r => r.Id, r => r.Object);
        public Dictionary<object, uint> CopyIdTable() => this.objectToReference.Take(this.objectToReferenceCount).ToDictionary(r => r.Object, r => r.Id);

        public uint CurrentReferenceId { get; set; }

        public void Reset()
        {
            for (var i = 0; i < this.referenceToObjectCount; i++)
            {
                this.referenceToObject[i] = default;
            }
            for (var i = 0; i < this.objectToReferenceCount; i++)
            {
                this.objectToReference[i] = default;
            }
            this.referenceToObjectCount = 0;
            this.objectToReferenceCount = 0;
            this.CurrentReferenceId = 0;
        }
    }
}