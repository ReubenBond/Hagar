using System;
using System.Collections.Generic;
using System.Linq;

namespace Hagar.Buffers
{
    internal class ByteArrayBuilder
    {
        private const int MINIMUM_BUFFER_SIZE = 256;
        private readonly int bufferSize;
        private readonly List<ArraySegment<byte>> completedBuffers;
        private byte[] currentBuffer;
        private int currentOffset;
        private int completedLength;
        private readonly BufferPool pool;

        // These arrays are all pre-allocated to avoid using BitConverter.GetBytes(), 
        // which allocates a byte array and thus has some perf overhead
        private readonly int[] tempIntArray = new int[1];
        private readonly uint[] tempUIntArray = new uint[1];
        private readonly short[] tempShortArray = new short[1];
        private readonly ushort[] tempUShortArray = new ushort[1];
        private readonly long[] tempLongArray = new long[1];
        private readonly ulong[] tempULongArray = new ulong[1];
        private readonly double[] tempDoubleArray = new double[1];
        private readonly float[] tempFloatArray = new float[1];

        /// <summary>
        /// 
        /// </summary>
        public ByteArrayBuilder()
            : this((BufferPool) BufferPool.GlobalPool)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param Name="size"></param>
        private ByteArrayBuilder(BufferPool bufferPool)
        {
            this.pool = bufferPool;
            this.bufferSize = bufferPool.Size;
            this.completedBuffers = new List<ArraySegment<byte>>();
            this.currentOffset = 0;
            this.completedLength = 0;
            this.currentBuffer = null;
        }

        public void ReleaseBuffers()
        {
            this.pool.Release(this.ToBytes());
            this.currentBuffer = null;
            this.currentOffset = 0;
        }

        public List<ArraySegment<byte>> ToBytes()
        {
            if (this.currentOffset <= 0) return this.completedBuffers;

            this.completedBuffers.Add(new ArraySegment<byte>(this.currentBuffer, 0, this.currentOffset));
            this.completedLength += this.currentOffset;
            this.currentBuffer = null;
            this.currentOffset = 0;

            return this.completedBuffers;
        }

        private bool RoomFor(int n)
        {
            return (this.currentBuffer != null) && (this.currentOffset + n <= this.bufferSize);
        }

        public byte[] ToByteArray()
        {
            var result = new byte[this.Length];

            int offset = 0;
            foreach (var buffer in this.completedBuffers)
            {
                Array.Copy(buffer.Array, buffer.Offset, result, offset, buffer.Count);
                offset += buffer.Count;
            }

            if ((this.currentOffset > 0) && (this.currentBuffer != null))
            {
                Array.Copy(this.currentBuffer, 0, result, offset, this.currentOffset);
            }

            return result;
        }

        public int Length
        {
            get
            {
                return this.currentOffset + this.completedLength;
            }
        }

        private void Grow()
        {
            if (this.currentBuffer != null)
            {
                this.completedBuffers.Add(new ArraySegment<byte>(this.currentBuffer, 0, this.currentOffset));
                this.completedLength += this.currentOffset;
            }
            this.currentBuffer = this.pool.GetBuffer();
            this.currentOffset = 0;
        }

        private void EnsureRoomFor(int n)
        {
            if (!this.RoomFor(n))
            {
                this.Grow();
            }
        }

        /// <summary>
        /// Append a byte array to the byte array.
        /// Note that this assumes that the array passed in is now owned by the ByteArrayBuilder, and will not be modified.
        /// </summary>
        /// <param Name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(byte[] array)
        {
            int arrLen = array.Length;
            // Big enough for its own buffer
            //
            // This is a somewhat debatable optimization:
            // 1) If the passed array is bigger than bufferSize, don't copy it and append it as its own buffer. 
            // 2) Make sure to ALWAYS copy arrays which size is EXACTLY bufferSize, otherwise if the data was passed as an Immutable arg, 
            // we may return this buffer back to the BufferPool and later over-write it.
            // 3) If we already have MINIMUM_BUFFER_SIZE in the current buffer and passed enough data, also skip the copy and append it as its own buffer. 
            if (((arrLen != this.bufferSize) && (this.currentOffset > MINIMUM_BUFFER_SIZE) && (arrLen > MINIMUM_BUFFER_SIZE)) || (arrLen > this.bufferSize))
            {
                this.Grow();
                this.completedBuffers.Add(new ArraySegment<byte>(array));
                this.completedLength += array.Length;
            }
            else
            {
                this.EnsureRoomFor(1);
                int n = Math.Min(array.Length, (int)(this.bufferSize - this.currentOffset));
                Array.Copy(array, 0, this.currentBuffer, this.currentOffset, n);
                this.currentOffset += n;
                int r = array.Length - n;
                if (r <= 0) return this;

                this.Grow(); // Resets currentOffset to zero
                Array.Copy(array, n, this.currentBuffer, this.currentOffset, r);
                this.currentOffset += r;
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(ByteArrayBuilder b)
        {
            if ((this.currentBuffer != null) && (this.currentOffset > 0))
            {
                this.completedBuffers.Add(new ArraySegment<byte>(this.currentBuffer, 0, this.currentOffset));
                this.completedLength += this.currentOffset;
            }

            this.completedBuffers.AddRange(b.completedBuffers);
            this.completedLength += b.completedLength;

            this.currentBuffer = b.currentBuffer;
            this.currentOffset = b.currentOffset;

            return this;
        }

        /// <summary>
        /// Append a list of byte array segments to the byte array.
        /// Note that this assumes that the data passed in is now owned by the ByteArrayBuilder, and will not be modified.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(List<ArraySegment<byte>> b)
        {
            if ((this.currentBuffer != null) && (this.currentOffset > 0))
            {
                this.completedBuffers.Add(new ArraySegment<byte>(this.currentBuffer, 0, this.currentOffset));
                this.completedLength += this.currentOffset;
            }

            this.completedBuffers.AddRange(b);
            this.completedLength += b.Sum(buff => buff.Count);

            this.currentBuffer = null;
            this.currentOffset = 0;

            return this;
        }

        private ByteArrayBuilder AppendImpl(Array array)
        {
            int n = Buffer.ByteLength(array);
            if (this.RoomFor(n))
            {
                Buffer.BlockCopy(array, 0, this.currentBuffer, this.currentOffset, n);
                this.currentOffset += n;
            }
            else if (n <= this.bufferSize)
            {
                this.Grow();
                Buffer.BlockCopy(array, 0, this.currentBuffer, this.currentOffset, n);
                this.currentOffset += n;
            }
            else
            {
                var pos = 0;
                while (pos < n)
                {
                    this.EnsureRoomFor(1);
                    var k = Math.Min(n - pos, (int) (this.bufferSize - this.currentOffset));
                    Buffer.BlockCopy(array, pos, this.currentBuffer, this.currentOffset, k);
                    pos += k;
                    this.currentOffset += k;
                }
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(short[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(int[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(long[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(ushort[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(uint[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(ulong[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(sbyte[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(char[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(bool[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(float[] array)
        {
            return this.AppendImpl(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ByteArrayBuilder Append(double[] array)
        {
            return this.AppendImpl(array);
        }

        public ByteArrayBuilder Append(byte b)
        {
            this.EnsureRoomFor(1);
            this.currentBuffer[this.currentOffset++] = b;
            return this;
        }
        
        public ByteArrayBuilder Append(sbyte b)
        {
            this.EnsureRoomFor(1);
            this.currentBuffer[this.currentOffset++] = unchecked((byte)b);
            return this;
        }

        public ByteArrayBuilder Append(int i)
        {
            this.EnsureRoomFor(sizeof(int));
            this.tempIntArray[0] = i;
            Buffer.BlockCopy(this.tempIntArray, 0, this.currentBuffer, this.currentOffset, sizeof(int));
            this.currentOffset += sizeof(int);
            return this;
        }

        public ByteArrayBuilder Append(long i)
        {
            this.EnsureRoomFor(sizeof(long));
            this.tempLongArray[0] = i;
            Buffer.BlockCopy(this.tempLongArray, 0, this.currentBuffer, this.currentOffset, sizeof(long));
            this.currentOffset += sizeof(long);
            return this;
        }

        public ByteArrayBuilder Append(short i)
        {
            this.EnsureRoomFor(sizeof(short));
            this.tempShortArray[0] = i;
            Buffer.BlockCopy(this.tempShortArray, 0, this.currentBuffer, this.currentOffset, sizeof(short));
            this.currentOffset += sizeof(short);
            return this;
        }

        public ByteArrayBuilder Append(uint i)
        {
            this.EnsureRoomFor(sizeof(uint));
            this.tempUIntArray[0] = i;
            Buffer.BlockCopy(this.tempUIntArray, 0, this.currentBuffer, this.currentOffset, sizeof(uint));
            this.currentOffset += sizeof(uint);
            return this;
        }

        public ByteArrayBuilder Append(ulong i)
        {
            this.EnsureRoomFor(sizeof(ulong));
            this.tempULongArray[0] = i;
            Buffer.BlockCopy(this.tempULongArray, 0, this.currentBuffer, this.currentOffset, sizeof(ulong));
            this.currentOffset += sizeof(ulong);
            return this;
        }

        public ByteArrayBuilder Append(ushort i)
        {
            this.EnsureRoomFor(sizeof(ushort));
            this.tempUShortArray[0] = i;
            Buffer.BlockCopy(this.tempUShortArray, 0, this.currentBuffer, this.currentOffset, sizeof(ushort));
            this.currentOffset += sizeof(ushort);
            return this;
        }

        public ByteArrayBuilder Append(float i)
        {
            this.EnsureRoomFor(sizeof(float));
            this.tempFloatArray[0] = i;
            Buffer.BlockCopy(this.tempFloatArray, 0, this.currentBuffer, this.currentOffset, sizeof(float));
            this.currentOffset += sizeof(float);
            return this;
        }

        public ByteArrayBuilder Append(double i)
        {
            this.EnsureRoomFor(sizeof(double));
            this.tempDoubleArray[0] = i;
            Buffer.BlockCopy(this.tempDoubleArray, 0, this.currentBuffer, this.currentOffset, sizeof(double));
            this.currentOffset += sizeof(double);
            return this;
        }


        // Utility function for manipulating lists of array segments
        public static List<ArraySegment<byte>> BuildSegmentList(List<ArraySegment<byte>> buffer, int offset)
        {
            if (offset == 0)
            {
                return buffer;
            }

            var result = new List<ArraySegment<byte>>();
            var lengthSoFar = 0;
            foreach (var segment in buffer)
            {
                var bytesStillToSkip = offset - lengthSoFar;
                lengthSoFar += segment.Count;
                if (segment.Count <= bytesStillToSkip) // Still skipping past this buffer
                {
                    continue;
                }
                if (bytesStillToSkip > 0) // This is the first buffer, so just take part of it
                {
                    result.Add(new ArraySegment<byte>(segment.Array, bytesStillToSkip, segment.Count - bytesStillToSkip));
                }
                else // Take the whole buffer
                {
                    result.Add(segment);
                }
            }
            return result;
        }

        // Utility function for manipulating lists of array segments
        public static List<ArraySegment<byte>> BuildSegmentListWithLengthLimit(List<ArraySegment<byte>> buffer, int offset, int length)
        {
            var result = new List<ArraySegment<byte>>();
            var lengthSoFar = 0;
            var countSoFar = 0;
            foreach (var segment in buffer)
            {
                var bytesStillToSkip = offset - lengthSoFar;
                lengthSoFar += segment.Count;

                if (segment.Count <= bytesStillToSkip) // Still skipping past this buffer
                {
                    continue;
                }
                if (bytesStillToSkip > 0) // This is the first buffer
                {
                    result.Add(new ArraySegment<byte>(segment.Array, bytesStillToSkip, Math.Min(length - countSoFar, segment.Count - bytesStillToSkip)));
                    countSoFar += Math.Min(length - countSoFar, segment.Count - bytesStillToSkip);
                }
                else
                {
                    result.Add(new ArraySegment<byte>(segment.Array, 0, Math.Min(length - countSoFar, segment.Count)));
                    countSoFar += Math.Min(length - countSoFar, segment.Count);
                }

                if (countSoFar == length)
                {
                    break;
                }
            }
            return result;
        }
    }
}