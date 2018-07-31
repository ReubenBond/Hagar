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
                Id = id;
                Object = @object;
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
                if (referenceToObject[i].Id == reference)
                {
                    value = referenceToObject[i].Object;
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
                if (ReferenceEquals(objectToReference[i].Object, value))
                {
                    reference = objectToReference[i].Id;
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
            if (objectToReferenceCount >= this.objectToReference.Length)
            {
                var old = objectToReference;
                objectToReference = new ReferencePair[objectToReference.Length * 2];
                Array.Copy(old, objectToReference, objectToReferenceCount);
            }

            this.objectToReference[objectToReferenceCount++] = new ReferencePair(reference, value);
        }

        private void AddToReferences(object value, uint reference)
        {
            if (referenceToObjectCount >= this.referenceToObject.Length)
            {
                var old = referenceToObject;
                referenceToObject = new ReferencePair[referenceToObject.Length * 2];
                Array.Copy(old, referenceToObject, referenceToObjectCount);
            }

            this.referenceToObject[referenceToObjectCount++] = new ReferencePair(reference, value);
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
            for (var i = 0; i < referenceToObjectCount; i++)
            {
                this.referenceToObject[i] = default;
            }
            for (var i = 0; i < objectToReferenceCount; i++)
            {
                this.objectToReference[i] = default;
            }
            this.referenceToObjectCount = 0;
            this.objectToReferenceCount = 0;
            this.CurrentReferenceId = 0;
        }
    }
}