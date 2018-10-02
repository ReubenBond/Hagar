using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Hagar.Session;

namespace Hagar.Buffers
{
    public static class BufferWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<TBufferWriter> CreateWriter<TBufferWriter>(this TBufferWriter buffer, SerializerSession session) where TBufferWriter : IBufferWriter<byte>
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return new Writer<TBufferWriter>(buffer, session);
        }
    }
}