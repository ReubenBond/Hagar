using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Hagar.TestKit
{
    public class TestMultiSegmentBufferWriter : IBufferWriter<byte>, IOutputBuffer
    {
        private readonly List<byte[]> committed = new List<byte[]>();
        private readonly int maxAllocationSize;
        private byte[] current = new byte[0];

        public TestMultiSegmentBufferWriter(int maxAllocationSize)
        {
            this.maxAllocationSize = maxAllocationSize;
        }

        public void Advance(int bytes)
        {
            if (bytes == 0)
            {
                return;
            }

            this.committed.Add(this.current.AsSpan(0, bytes).ToArray());
            this.current = new byte[0];
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint == 0)
                sizeHint = this.current.Length + 1;
            if (sizeHint < this.current.Length)
                throw new InvalidOperationException("Attempted to allocate a new buffer when the existing buffer has sufficient free space.");
            var newBuffer = new byte[Math.Min(sizeHint, this.maxAllocationSize)];
            this.current.CopyTo(newBuffer.AsSpan());
            this.current = newBuffer;
            return this.current;
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            if (sizeHint == 0)
                sizeHint = this.current.Length + 1;
            if (sizeHint < this.current.Length)
                throw new InvalidOperationException("Attempted to allocate a new buffer when the existing buffer has sufficient free space.");
            var newBuffer = new byte[Math.Min(sizeHint, this.maxAllocationSize)];
            this.current.CopyTo(newBuffer.AsSpan());
            this.current = newBuffer;
            return this.current;
        }

        [Pure]
        public ReadOnlySequence<byte> GetReadOnlySequence(int maxSegmentSize)
        {
            return this.committed.SelectMany(b => b).Batch(maxSegmentSize).ToReadOnlySequence();
        }
    }
}