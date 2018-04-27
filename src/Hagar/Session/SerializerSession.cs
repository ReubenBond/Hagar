using System;
using Hagar.TypeSystem;

namespace Hagar.Session
{
    public sealed class SerializerSession : IDisposable
    {
        public SerializerSession(TypeCodec typeCodec, WellKnownTypeCollection wellKnownTypes)
        {
            this.TypeCodec = typeCodec;
            this.WellKnownTypes = wellKnownTypes;
        }

        public TypeCodec TypeCodec { get; }
        public WellKnownTypeCollection WellKnownTypes { get; }
        public ReferencedTypeCollection ReferencedTypes { get; } = new ReferencedTypeCollection();
        public ReferencedObjectCollection ReferencedObjects { get; } = new ReferencedObjectCollection();

        internal Action<SerializerSession> OnDisposed { get; set; }

        public void PartialReset()
        {
            this.ReferencedObjects.Reset();
        }

        public void FullReset()
        {
            this.ReferencedObjects.Reset();
            this.ReferencedTypes.Reset();
        }

        public void Dispose() => this.OnDisposed?.Invoke(this);
    }
}