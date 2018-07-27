using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;

namespace Hagar.Buffers
{
    public class Writer : Writer<PipeWriter>
    {
        public Writer(PipeWriter output) : base(output)
        {
        }
    }

    public class Writer<TWriter> where TWriter : IBufferWriter<byte>
    {
        private readonly TWriter output;
        private int totalLength;

        /// <summary> Default constructor. </summary>
        public Writer(TWriter output)
        {
            this.output = output;
        }

        public int TotalLength => this.totalLength;

        private void Advance(int length)
        {
            this.totalLength += length;
            this.output.Advance(length);
        }

        /// <summary> Write an <c>Int32</c> value to the stream. </summary>
        public void Write(int i)
        {
            var span = this.output.GetSpan(4);
            BinaryPrimitives.WriteInt32LittleEndian(span, i);
            this.Advance(4);
        }

        /// <summary> Write an <c>Int16</c> value to the stream. </summary>
        public void Write(short s)
        {
            var span = this.output.GetSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(span, s);
            this.Advance(2);
        }

        /// <summary> Write an <c>Int64</c> value to the stream. </summary>
        public void Write(long l)
        {
            var span = this.output.GetSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(span, l);
            this.Advance(8);
        }

        /// <summary> Write a <c>sbyte</c> value to the stream. </summary>
        public void Write(sbyte b)
        {
            var span = this.output.GetSpan(1);
            span[0] = (byte)b;
            this.Advance(1);
        }

        /// <summary> Write a <c>UInt32</c> value to the stream. </summary>
        public void Write(uint u)
        {
            var span = this.output.GetSpan(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, u);
            this.Advance(4);
        }

        /// <summary> Write a <c>UInt16</c> value to the stream. </summary>
        public void Write(ushort u)
        {
            var span = this.output.GetSpan(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, u);
            this.Advance(2);
        }

        /// <summary> Write a <c>UInt64</c> value to the stream. </summary>
        public void Write(ulong u)
        {
            var span = this.output.GetSpan(8);
            BinaryPrimitives.WriteUInt64LittleEndian(span, u);
            this.Advance(8);
        }

        /// <summary> Write a <c>byte</c> value to the stream. </summary>
        public void Write(byte b)
        {
            var span = this.output.GetSpan(1);
            span[0] = b;
            this.Advance(1);
        }

#if NETCOREAPP2_1
        public void Write(float value) => WriteRawLittleEndian32((uint)BitConverter.SingleToInt32Bits(value));
#else
        public void Write(float value) => this.Write((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
#endif

        public void Write(double value)
        {
            var span = this.output.GetSpan(8);
            MemoryMarshal.Write(span, ref value);
            this.Advance(8);
        }

        /// <summary> Write a <c>decimal</c> value to the stream. </summary>
        public void Write(decimal d)
        {
            var ints = Decimal.GetBits(d);
            foreach (var part in ints) this.Write(part);
        }

        /// <summary> Write a <c>string</c> value to the stream. </summary>
        public void Write(string s)
        {
            if (null == s)
            {
                this.Write(-1);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                this.Write(bytes.Length);
                this.Write(bytes);
            }
        }

        /// <summary> Write a <c>char</c> value to the stream. </summary>
        public void Write(char c)
        {
            this.Write(Convert.ToInt16(c));
        }

        /// <summary> Write a <c>byte[]</c> value to the stream. </summary>
        public void Write(byte[] b)
        {
            this.Write(new ReadOnlySpan<byte>(b));
        }

        public void Write(ReadOnlySpan<byte> input)
        {
            var remaining = input;
            while (remaining.Length > 0)
            {
                // Get a span which is hopefully large enough to fit the remaining input.
                var outputSpan = this.output.GetSpan();

                var len = Math.Min(remaining.Length, outputSpan.Length);

                // Copy from input to output.
                remaining.Slice(0, len).CopyTo(outputSpan);

                // Advance the input.
                remaining = remaining.Slice(len);
                this.Advance(len);
            }
        }

        /// <summary> Write the specified number of bytes to the stream, starting at the specified offset in the input <c>byte[]</c>. </summary>
        /// <param name="b">The input data to be written.</param>
        /// <param name="offset">The offset into the inout byte[] to start writing bytes from.</param>
        /// <param name="count">The number of bytes to be written.</param>
        public void Write(byte[] b, int offset, int count)
        {
            Write(new ReadOnlySpan<byte>(b, offset, count));
        }

        /// <summary> Write a <c>TimeSpan</c> value to the stream. </summary>
        public void Write(TimeSpan ts)
        {
            this.Write(ts.Ticks);
        }

        /// <summary> Write a <c>DataTime</c> value to the stream. </summary>
        public void Write(DateTime dt)
        {
            this.Write(dt.ToBinary());
        }

        /// <summary> Write a <c>Guid</c> value to the stream. </summary>
        public void Write(Guid id)
        {
            this.Write(id.ToByteArray());
        }
    }
}