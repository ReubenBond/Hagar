using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hagar.Buffers
{
    public static class BufferWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<TBufferWriter> CreateWriter<TBufferWriter>(this TBufferWriter buffer) where TBufferWriter : IBufferWriter<byte> => new Writer<TBufferWriter>(buffer);
    }
}