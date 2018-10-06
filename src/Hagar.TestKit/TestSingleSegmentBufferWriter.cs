using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Hagar.TestKit
{
    public class TestSingleSegmentBufferWriter : IBufferWriter<byte>, IOutputBuffer
    {
        private readonly byte[] buffer;
        private int written;

        public TestSingleSegmentBufferWriter(byte[] buffer)
        {
            this.buffer = buffer;
            this.written = 0;
        }

        public void Advance(int bytes)
        {
            this.written += bytes;
        }

        [Pure]
        public Memory<byte> GetMemory(int sizeHint = 0) => this.buffer.AsMemory().Slice(this.written);

        [Pure]
        public Span<byte> GetSpan(int sizeHint) => this.buffer.AsSpan().Slice(this.written);

        [Pure]
        public int Length => this.written;

        [Pure]
        public ReadOnlySequence<byte> GetReadOnlySequence(int maxSegmentSize)
        {
            return this.buffer.Take(this.written).Batch(maxSegmentSize).ToReadOnlySequence();
        }
    }
}