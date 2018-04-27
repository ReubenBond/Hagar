using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hagar.Buffers
{
    /// <summary>
    /// Reader for Orleans binary token streams
    /// </summary>
    public class Reader
    {
        private List<ArraySegment<byte>> buffers;
        private int buffersCount;
        private int currentSegmentIndex;
        private ArraySegment<byte> currentSegment;
        private byte[] currentBuffer;
        private int currentOffset;
        private int currentSegmentOffset;
        private int currentSegmentCount;
        private int totalProcessedBytes;
        private int currentSegmentOffsetPlusCount;
        private int totalLength;

        private static readonly ArraySegment<byte> emptySegment = new ArraySegment<byte>(new byte[0]);
        private static readonly byte[] emptyByteArray = new byte[0];

        /// <summary>
        /// Create a new BinaryTokenStreamReader to read from the specified input byte array.
        /// </summary>
        /// <param name="input">Input binary data to be tokenized.</param>
        public Reader(byte[] input)
            : this(new List<ArraySegment<byte>> { new ArraySegment<byte>(input) })
        {
        }

        /// <summary>
        /// Create a new BinaryTokenStreamReader to read from the specified input buffers.
        /// </summary>
        /// <param name="buffs">The list of ArraySegments to use for the data.</param>
        public Reader(List<ArraySegment<byte>> buffs)
        {
            this.Reset(buffs);
            this.Trace("Starting new stream reader");
        }

        /// <summary>
        /// Resets this instance with the provided data.
        /// </summary>
        /// <param name="buffs">The underlying buffers.</param>
        public void Reset(List<ArraySegment<byte>> buffs)
        {
            this.buffers = buffs;
            this.totalProcessedBytes = 0;
            this.currentSegmentIndex = 0;
            this.InitializeCurrentSegment(0);
            this.totalLength = buffs.Sum(b => b.Count);
            this.buffersCount = buffs.Count;
        }

        private void InitializeCurrentSegment(int segmentIndex)
        {
            this.currentSegment = this.buffers[segmentIndex];
            this.currentBuffer = this.currentSegment.Array;
            this.currentOffset = this.currentSegment.Offset;
            this.currentSegmentOffset = this.currentOffset;
            this.currentSegmentCount = this.currentSegment.Count;
            this.currentSegmentOffsetPlusCount = this.currentSegmentOffset + this.currentSegmentCount;
        }

        /// <summary>
        /// Create a new BinaryTokenStreamReader to read from the specified input buffer.
        /// </summary>
        /// <param name="buff">ArraySegment to use for the data.</param>
        public Reader(ArraySegment<byte> buff)
            : this(
                new List<ArraySegment<byte>>
                {
                    buff
                })
        {
        }

        /// <summary> Current read position in the stream. </summary>
        public int CurrentPosition => this.currentOffset + this.totalProcessedBytes - this.currentSegmentOffset;

        /// <summary>
        /// Gets the total length.
        /// </summary>
        public int Length => this.totalLength;

        /// <summary>
        /// Creates a copy of the current stream reader.
        /// </summary>
        /// <returns>The new copy</returns>
        public List<ArraySegment<byte>> GetBuffers()
        {
            return this.buffers;
        }

        public void Advance(int bytes)
        {
            this.CheckLength(bytes, out int offset);
        }

        private void StartNextSegment()
        {
            this.totalProcessedBytes += this.currentSegment.Count;
            this.currentSegmentIndex++;
            if (this.currentSegmentIndex < this.buffersCount)
            {
                this.InitializeCurrentSegment(this.currentSegmentIndex);
            }
            else
            {
                this.currentSegment = emptySegment;
                this.currentBuffer = null;
                this.currentOffset = 0;
                this.currentSegmentOffset = 0;
                this.currentSegmentOffsetPlusCount = this.currentSegmentOffset + this.currentSegmentCount;
            }
        }

        private byte[] CheckLength(int n, out int offset)
        {
            bool ignore;
            byte[] res;
            if (this.TryCheckLengthFast(n, out res, out offset, out ignore))
            {
                return res;
            }

            return this.CheckLength(n, out offset, out ignore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryCheckLengthFast(int n, out byte[] res, out int offset, out bool safeToUse)
        {
            safeToUse = false;
            res = null;
            offset = 0;
            var nextOffset = this.currentOffset + n;
            if (nextOffset <= this.currentSegmentOffsetPlusCount)
            {
                offset = this.currentOffset;
                this.currentOffset = nextOffset;
                res = this.currentBuffer;
                return true;
            }

            return false;
        }

        private byte[] CheckLength(int n, out int offset, out bool safeToUse)
        {
            safeToUse = false;
            offset = 0;
            if (this.currentOffset == this.currentSegmentOffsetPlusCount)
            {
                this.StartNextSegment();
            }

            byte[] res;
            if (this.TryCheckLengthFast(n, out res, out offset, out safeToUse))
            {
                return res;
            }

            if ((this.CurrentPosition + n > this.totalLength))
            {
                throw new SerializationException(
                    String.Format("Attempt to read past the end of the input stream: CurrentPosition={0}, n={1}, totalLength={2}",
                                  this.CurrentPosition, n, this.totalLength));
            }

            var temp = new byte[n];
            var i = 0;

            while (i < n)
            {
                var segmentOffsetPlusCount = this.currentSegmentOffsetPlusCount;
                var bytesFromThisBuffer = Math.Min((int) (segmentOffsetPlusCount - this.currentOffset), n - i);
                Buffer.BlockCopy(this.currentBuffer, this.currentOffset, temp, i, bytesFromThisBuffer);
                i += bytesFromThisBuffer;
                this.currentOffset += bytesFromThisBuffer;
                if (this.currentOffset >= segmentOffsetPlusCount)
                {
                    if (this.currentSegmentIndex >= this.buffersCount)
                    {
                        throw new SerializationException(
                            String.Format("Attempt to read past buffers.Count: currentSegmentIndex={0}, buffers.Count={1}.", this.currentSegmentIndex, this.buffers.Count));
                    }

                    this.StartNextSegment();
                }
            }
            safeToUse = true;
            offset = 0;
            return temp;
        }
        
        public byte ReadByte()
        {
            int offset;
            var buff = this.CheckLength(sizeof(byte), out offset);
            var val = buff[offset];
            this.Trace("--Read byte {0}", val);
            return val;
        }

        /// <summary> Read an <c>Int32</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public int ReadInt()
        {
            int offset;
            var buff = this.CheckLength(sizeof(int), out offset);
            var val = BitConverter.ToInt32(buff, offset);
            this.Trace("--Read int {0}", val);
            return val;
        }

        /// <summary> Read an <c>UInt32</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public uint ReadUInt()
        {
            int offset;
            var buff = this.CheckLength(sizeof(uint), out offset);
            var val = BitConverter.ToUInt32(buff, offset);
            this.Trace("--Read uint {0}", val);
            return val;
        }

        /// <summary> Read an <c>Int16</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public short ReadShort()
        {
            int offset;
            var buff = this.CheckLength(sizeof(short), out offset);
            var val = BitConverter.ToInt16(buff, offset);
            this.Trace("--Read short {0}", val);
            return val;
        }

        /// <summary> Read an <c>UInt16</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public ushort ReadUShort()
        {
            int offset;
            var buff = this.CheckLength(sizeof(ushort), out offset);
            var val = BitConverter.ToUInt16(buff, offset);
            this.Trace("--Read ushort {0}", val);
            return val;
        }

        /// <summary> Read an <c>Int64</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public long ReadLong()
        {
            int offset;
            var buff = this.CheckLength(sizeof(long), out offset);
            var val = BitConverter.ToInt64(buff, offset);
            this.Trace("--Read long {0}", val);
            return val;
        }

        /// <summary> Read an <c>UInt64</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public ulong ReadULong()
        {
            int offset;
            var buff = this.CheckLength(sizeof(ulong), out offset);
            var val = BitConverter.ToUInt64(buff, offset);
            this.Trace("--Read ulong {0}", val);
            return val;
        }

        /// <summary> Read an <c>float</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public float ReadFloat()
        {
            int offset;
            var buff = this.CheckLength(sizeof(float), out offset);
            var val = BitConverter.ToSingle(buff, offset);
            this.Trace("--Read float {0}", val);
            return val;
        }

        /// <summary> Read an <c>double</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public double ReadDouble()
        {
            int offset;
            var buff = this.CheckLength(sizeof(double), out offset);
            var val = BitConverter.ToDouble(buff, offset);
            this.Trace("--Read double {0}", val);
            return val;
        }

        /// <summary> Read an <c>decimal</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public decimal ReadDecimal()
        {
            int offset;
            var buff = this.CheckLength(4 * sizeof(int), out offset);
            var raw = new int[4];
            this.Trace("--Read decimal");
            var n = offset;
            for (var i = 0; i < 4; i++)
            {
                raw[i] = BitConverter.ToInt32(buff, n);
                n += sizeof(int);
            }
            return new decimal(raw);
        }

        public DateTime ReadDateTime()
        {
            var n = this.ReadLong();
            return n == 0 ? default(DateTime) : DateTime.FromBinary(n);
        }

        /// <summary> Read an <c>string</c> value from the stream. </summary>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public string ReadString()
        {
            var n = this.ReadInt();
            if (n == 0)
            {
                this.Trace("--Read empty string");
                return String.Empty;
            }

            string s = null;
            // a length of -1 indicates that the string is null.
            if (-1 != n)
            {
                int offset;
                var buff = this.CheckLength(n, out offset);
                s = Encoding.UTF8.GetString(buff, offset, n);
            }

            this.Trace("--Read string '{0}'", s);
            return s;
        }

        /// <summary> Read the next bytes from the stream. </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Data from current position in stream, converted to the appropriate output type.</returns>
        public byte[] ReadBytes(int count)
        {
            if (count == 0)
            {
                return emptyByteArray;
            }
            bool safeToUse;

            int offset;
            byte[] buff;
            if (!this.TryCheckLengthFast(count, out buff, out offset, out safeToUse))
            {
                buff = this.CheckLength(count, out offset, out safeToUse);
            }

            this.Trace("--Read byte array of length {0}", count);
            if (!safeToUse)
            {
                var result = new byte[count];
                Array.Copy(buff, offset, result, 0, count);
                return result;
            }
            else
            {
                return buff;
            }
        }

        /// <summary> Read the next bytes from the stream. </summary>
        /// <param name="destination">Output array to store the returned data in.</param>
        /// <param name="offset">Offset into the destination array to write to.</param>
        /// <param name="count">Number of bytes to read.</param>
        public void ReadByteArray(byte[] destination, int offset, int count)
        {
            if (offset + count > destination.Length)
            {
                throw new ArgumentOutOfRangeException("count", "Reading into an array that is too small");
            }

            var buffOffset = 0;
            var buff = count == 0 ? emptyByteArray : this.CheckLength(count, out buffOffset);
            Buffer.BlockCopy(buff, buffOffset, destination, offset, count);
        }
        
        /// <summary>
        /// Read a block of data into the specified output <c>Array</c>.
        /// </summary>
        /// <param name="array">Array to output the data to.</param>
        /// <param name="n">Number of bytes to read.</param>
        public void ReadBlockInto(Array array, int n)
        {
            int offset;
            var buff = this.CheckLength(n, out offset);
            Buffer.BlockCopy(buff, offset, array, 0, n);
            this.Trace("--Read block of {0} bytes", n);
        }

        private StreamWriter trace;

        [Conditional("TRACE_SERIALIZATION")]
        private void Trace(string format, params object[] args)
        {
            if (this.trace == null)
            {
                var path = String.Format("d:\\Trace-{0}.{1}.{2}.txt", DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Ticks);
                Console.WriteLine("Opening trace file at '{0}'", path);
                this.trace = File.CreateText(path);
            }
            this.trace.Write(format, args);
            this.trace.WriteLine(" at offset {0}", this.CurrentPosition);
            this.trace.Flush();
        }
    }

    internal class SerializationException : Exception
    {
        public SerializationException(string format) : base(format)
        {
        }
    }
}