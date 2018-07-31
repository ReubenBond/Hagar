using System;
using System.Buffers;
using System.Text;

namespace Benchmarks
{
    public class SingleSegmentBuffer : IBufferWriter<byte>
    {
        private readonly byte[] buffer = new byte[1000];
        private int written;

        public void Advance(int bytes)
        {
            written += bytes;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => buffer.AsMemory().Slice(written);

        public Span<byte> GetSpan(int sizeHint) => buffer.AsSpan().Slice(written);
        public byte[] ToArray() => buffer.AsSpan(0, written).ToArray();

        public void Reset() => this.written = 0;

        public int Length => this.written;

        public ReadOnlySequence<byte> GetReadOnlySequence() => new ReadOnlySequence<byte>(this.buffer, 0, this.written);

        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer.AsSpan(0, written).ToArray());
        }
    }
}