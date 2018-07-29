using System;
using Hagar.Serializers;
using Hagar.TypeSystem;

namespace Hagar.Session
{
    public sealed class SerializerSession : IDisposable
    {
        public SerializerSession(TypeCodec typeCodec, WellKnownTypeCollection wellKnownTypes, CodecProvider codecProvider)
        {
            this.TypeCodec = typeCodec;
            this.WellKnownTypes = wellKnownTypes;
            this.CodecProvider = codecProvider;
        }

        public TypeCodec TypeCodec { get; }
        public WellKnownTypeCollection WellKnownTypes { get; }
        public ReferencedTypeCollection ReferencedTypes { get; } = new ReferencedTypeCollection();
        public ReferencedObjectCollection ReferencedObjects { get; } = new ReferencedObjectCollection();
        public CodecProvider CodecProvider { get; }
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