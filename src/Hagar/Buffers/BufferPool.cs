using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Hagar.Buffers
{
    internal class BufferPool
    {
        private readonly int byteBufferSize;
        private readonly int maxBuffersCount;
        private readonly bool limitBuffersCount;
        private readonly ConcurrentBag<byte[]> buffers;

        private int currentBufferCount;

        public static BufferPool GlobalPool = new BufferPool(4 * 1024, 10000, 250, "Global");

        public int Size
        {
            get { return this.byteBufferSize; }
        }

        public int Count
        {
            get { return this.buffers.Count; }
        }

        public string Name
        {
            get;
            private set;
        }

        internal static void InitGlobalBufferPool()
        {
            GlobalPool = new BufferPool(4 * 1024, 10000, 250, "Global");
        }

        /// <summary>
        /// Creates a buffer pool.
        /// </summary>
        /// <param name="bufferSize">The size, in bytes, of each buffer.</param>
        /// <param name="maxBuffers">The maximum number of buffers to keep around, unused; by default, the number of unused buffers is unbounded.</param>
        /// <param name="preallocationSize">Initial number of buffers to allocate.</param>
        /// <param name="name">Name of the buffer pool.</param>
        private BufferPool(int bufferSize, int maxBuffers, int preallocationSize, string name)
        {
            this.Name = name;
            this.byteBufferSize = bufferSize;
            this.maxBuffersCount = maxBuffers;
            this.limitBuffersCount = maxBuffers > 0;
            this.buffers = new ConcurrentBag<byte[]>();

            if (preallocationSize <= 0) return;

            var dummy = this.GetMultiBuffer(preallocationSize * this.Size);
            this.Release(dummy);
        }

        public byte[] GetBuffer()
        {
            byte[] buffer;
            if (!this.buffers.TryTake(out buffer))
            {
                buffer = new byte[this.byteBufferSize];
            }
            else if (this.limitBuffersCount)
            {
                Interlocked.Decrement(ref this.currentBufferCount);
            }
            
            return buffer;
        }

        public List<ArraySegment<byte>> GetMultiBuffer(int totalSize)
        {
            var list = new List<ArraySegment<byte>>();
            while (totalSize > 0)
            {
                var buff = this.GetBuffer();
                list.Add(new ArraySegment<byte>(buff, 0, Math.Min((int) this.byteBufferSize, totalSize)));
                totalSize -= this.byteBufferSize;
            }
            return list;
        }

        public void Release(byte[] buffer)
        {
            if (buffer.Length == this.byteBufferSize)
            {
                if (this.limitBuffersCount && this.currentBufferCount > this.maxBuffersCount)
                {
                    return;
                }

                this.buffers.Add(buffer);

                if (this.limitBuffersCount)
                {
                    Interlocked.Increment(ref this.currentBufferCount);
                }
            }
        }

        public void Release(List<ArraySegment<byte>> list)
        {
            if (list == null) return;

            foreach (var segment in list)
            {
                this.Release(segment.Array);
            }
        }
    }
}