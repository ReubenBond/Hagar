using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Hagar.TestKit
{
    [ExcludeFromCodeCoverage]
    public struct TestBufferWriterStruct : IBufferWriter<byte>, IOutputBuffer
    {
        private readonly byte[] buffer;
        private int written;

        public TestBufferWriterStruct(byte[] buffer)
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
        public ReadOnlySequence<byte> GetReadOnlySequence(int maxSegmentSize)
        {
            return this.buffer.Take(this.written).Batch(maxSegmentSize).ToReadOnlySequence();
        }
    }
}