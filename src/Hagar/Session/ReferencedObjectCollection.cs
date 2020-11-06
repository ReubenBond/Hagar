using Hagar.Codecs;
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

        public int ReferenceToObjectCount { get; set; }
        private ReferencePair[] _referenceToObject = new ReferencePair[64];

        private int _objectToReferenceCount;
        private ReferencePair[] _objectToReference = new ReferencePair[64];

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
            for (int i = 0; i < ReferenceToObjectCount; ++i)
            {
                if (_referenceToObject[i].Id == reference)
                {
                    value = _referenceToObject[i].Object;
                    return true;
                }
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkValueField() => ++CurrentReferenceId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetOrAddReference(object value, out uint reference)
        {
            // Unconditionally bump the reference counter since a call to this method signifies a potential reference.
            var nextReference = ++CurrentReferenceId;

            // Null is always at reference 0
            if (value is null)
            {
                reference = 0;
                return true;
            }

            // TODO: Binary search
            for (int i = 0; i < _objectToReferenceCount; ++i)
            {
                if (ReferenceEquals(_objectToReference[i].Object, value))
                {
                    reference = _objectToReference[i].Id;
                    return true;
                }
            }

            // Add the reference.
            reference = nextReference;
            AddToReferenceToIdMap(value, reference);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetReferenceIndex(object value)
        {
            if (value is null)
            {
                return -1;
            }

            // TODO: Binary search
            for (var i = 0; i < ReferenceToObjectCount; ++i)
            {
                if (ReferenceEquals(_referenceToObject[i].Object, value))
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddToReferenceToIdMap(object value, uint reference)
        {
            if (_objectToReferenceCount >= _objectToReference.Length)
            {
                var old = _objectToReference;
                _objectToReference = new ReferencePair[_objectToReference.Length * 2];
                Array.Copy(old, _objectToReference, _objectToReferenceCount);
            }

            _objectToReference[_objectToReferenceCount++] = new ReferencePair(reference, value);
        }

        private void AddToReferences(object value, uint reference)
        {
            if (ReferenceToObjectCount >= _referenceToObject.Length)
            {
                GrowReferenceToObjectArray();
            }

            if (TryGetReferencedObject(reference, out var existing) && !(existing is UnknownFieldMarker) && !(value is UnknownFieldMarker))
            {
                // Unknown field markers can be replaced once the type is known.
                ThrowReferenceExistsException(reference);
                return;
            }

            _referenceToObject[ReferenceToObjectCount++] = new ReferencePair(reference, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowReferenceToObjectArray()
        {
            var old = _referenceToObject;
            _referenceToObject = new ReferencePair[_referenceToObject.Length * 2];
            Array.Copy(old, _referenceToObject, ReferenceToObjectCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReferenceExistsException(uint reference) => throw new InvalidOperationException($"Reference {reference} already exists");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordReferenceField(object value) => RecordReferenceField(value, ++CurrentReferenceId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordReferenceField(object value, uint referenceId)
        {
            if (value is null)
            {
                return;
            }

            AddToReferences(value, referenceId);
        }

        public Dictionary<uint, object> CopyReferenceTable() => _referenceToObject.Take(ReferenceToObjectCount).ToDictionary(r => r.Id, r => r.Object);
        public Dictionary<object, uint> CopyIdTable() => _objectToReference.Take(_objectToReferenceCount).ToDictionary(r => r.Object, r => r.Id);

        public uint CurrentReferenceId { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            var refToObj = _referenceToObject.AsSpan(0, Math.Min(_referenceToObject.Length, ReferenceToObjectCount));
            for (var i = 0; i < refToObj.Length; i++)
            {
                refToObj[i] = default;
            }

            var objToRef = _objectToReference.AsSpan(0, Math.Min(_objectToReference.Length, _objectToReferenceCount));
            for (var i = 0; i < objToRef.Length; i++)
            {
                objToRef[i] = default;
            }

            ReferenceToObjectCount = 0;
            _objectToReferenceCount = 0;
            CurrentReferenceId = 0;
        }
    }
}