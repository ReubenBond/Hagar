using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Text;

namespace Benchmarks.Utilities
{
    public struct SingleSegmentBuffer : IBufferWriter<byte>
    {
        private readonly byte[] buffer;
        private int written;

        public SingleSegmentBuffer(byte[] buffer)
        {
            this.buffer = buffer;
            this.written = 0;
        }

        public void Advance(int bytes)
        {
            written += bytes;
        }

        [Pure]
        public Memory<byte> GetMemory(int sizeHint = 0) => buffer.AsMemory().Slice(written);

        [Pure]
        public Span<byte> GetSpan(int sizeHint) => buffer.AsSpan().Slice(written);

        public byte[] ToArray() => buffer.AsSpan(0, written).ToArray();

        public void Reset() => this.written = 0;

        [Pure]
        public int Length => this.written;
        
        [Pure]
        public ReadOnlySequence<byte> GetReadOnlySequence() => new ReadOnlySequence<byte>(this.buffer, 0, this.written);

        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer.AsSpan(0, written).ToArray());
        }
    }
}