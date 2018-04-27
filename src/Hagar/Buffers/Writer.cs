using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Hagar.Buffers
{
    public class Writer
    {
        private readonly ByteArrayBuilder ab;

        /// <summary> Default constructor. </summary>
        public Writer()
        {
            this.ab = new ByteArrayBuilder();
            this.Trace("Starting new binary token stream");
        }

        /// <summary> Return the output stream as a set of <c>ArraySegment</c>. </summary>
        /// <returns>Data from this stream, converted to output type.</returns>
        public List<ArraySegment<byte>> ToBytes()
        {
            return this.ab.ToBytes();
        }

        /// <summary> Return the output stream as a <c>byte[]</c>. </summary>
        /// <returns>Data from this stream, converted to output type.</returns>
        public byte[] ToByteArray()
        {
            return this.ab.ToByteArray();
        }

        /// <summary> Release any serialization buffers being used by this stream. </summary>
        public void ReleaseBuffers()
        {
            this.ab.ReleaseBuffers();
        }

        /// <summary> Current write position in the stream. </summary>
        public int CurrentOffset
        {
            get { return this.ab.Length; }
        }

        /// <summary> Write an <c>Int32</c> value to the stream. </summary>
        public void Write(int i)
        {
            this.Trace("--Wrote integer {0}", i);
            this.ab.Append(i);
        }

        /// <summary> Write an <c>Int16</c> value to the stream. </summary>
        public void Write(short s)
        {
            this.Trace("--Wrote short {0}", s);
            this.ab.Append(s);
        }

        /// <summary> Write an <c>Int64</c> value to the stream. </summary>
        public void Write(long l)
        {
            this.Trace("--Wrote long {0}", l);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>sbyte</c> value to the stream. </summary>
        public void Write(sbyte b)
        {
            this.Trace("--Wrote sbyte {0}", b);
            this.ab.Append(b);
        }

        /// <summary> Write a <c>UInt32</c> value to the stream. </summary>
        public void Write(uint u)
        {
            this.Trace("--Wrote uint {0}", u);
            this.ab.Append(u);
        }

        /// <summary> Write a <c>UInt16</c> value to the stream. </summary>
        public void Write(ushort u)
        {
            this.Trace("--Wrote ushort {0}", u);
            this.ab.Append(u);
        }

        /// <summary> Write a <c>UInt64</c> value to the stream. </summary>
        public void Write(ulong u)
        {
            this.Trace("--Wrote ulong {0}", u);
            this.ab.Append(u);
        }

        /// <summary> Write a <c>byte</c> value to the stream. </summary>
        public void Write(byte b)
        {
            this.Trace("--Wrote byte {0}", b);
            this.ab.Append(b);
        }

        /// <summary> Write a <c>float</c> value to the stream. </summary>
        public void Write(float f)
        {
            this.Trace("--Wrote float {0}", f);
            this.ab.Append(f);
        }

        /// <summary> Write a <c>double</c> value to the stream. </summary>
        public void Write(double d)
        {
            this.Trace("--Wrote double {0}", d);
            this.ab.Append(d);
        }

        /// <summary> Write a <c>decimal</c> value to the stream. </summary>
        public void Write(decimal d)
        {
            this.Trace("--Wrote decimal {0}", d);
            this.ab.Append(Decimal.GetBits(d));
        }

        // Text

        /// <summary> Write a <c>string</c> value to the stream. </summary>
        public void Write(string s)
        {
            this.Trace("--Wrote string '{0}'", s);
            if (null == s)
            {
                this.ab.Append(-1);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                this.ab.Append(bytes.Length);
                this.ab.Append(bytes);
            }
        }

        /// <summary> Write a <c>char</c> value to the stream. </summary>
        public void Write(char c)
        {
            this.Trace("--Wrote char {0}", c);
            this.ab.Append(Convert.ToInt16(c));
        }

        // Primitive arrays

        /// <summary> Write a <c>byte[]</c> value to the stream. </summary>
        public void Write(byte[] b)
        {
            this.Trace("--Wrote byte array of length {0}", b.Length);
            this.ab.Append(b);
        }

        /// <summary> Write a list of byte array segments to the stream. </summary>
        public void Write(List<ArraySegment<byte>> bytes)
        {
            this.ab.Append(bytes);
        }

        /// <summary> Write the specified number of bytes to the stream, starting at the specified offset in the input <c>byte[]</c>. </summary>
        /// <param name="b">The input data to be written.</param>
        /// <param name="offset">The offset into the inout byte[] to start writing bytes from.</param>
        /// <param name="count">The number of bytes to be written.</param>
        public void Write(byte[] b, int offset, int count)
        {
            if (count <= 0)
            {
                return;
            }
            this.Trace("--Wrote byte array of length {0}", count);
            if ((offset == 0) && (count == b.Length))
            {
                this.Write(b);
            }
            else
            {
                var temp = new byte[count];
                Buffer.BlockCopy(b, offset, temp, 0, count);
                this.Write(temp);
            }
        }

        /// <summary> Write a <c>Int16[]</c> value to the stream. </summary>
        public void Write(short[] i)
        {
            this.Trace("--Wrote short array of length {0}", i.Length);
            this.ab.Append(i);
        }

        /// <summary> Write a <c>Int32[]</c> value to the stream. </summary>
        public void Write(int[] i)
        {
            this.Trace("--Wrote short array of length {0}", i.Length);
            this.ab.Append(i);
        }

        /// <summary> Write a <c>Int64[]</c> value to the stream. </summary>
        public void Write(long[] l)
        {
            this.Trace("--Wrote long array of length {0}", l.Length);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>UInt16[]</c> value to the stream. </summary>
        public void Write(ushort[] i)
        {
            this.Trace("--Wrote ushort array of length {0}", i.Length);
            this.ab.Append(i);
        }

        /// <summary> Write a <c>UInt32[]</c> value to the stream. </summary>
        public void Write(uint[] i)
        {
            this.Trace("--Wrote uint array of length {0}", i.Length);
            this.ab.Append(i);
        }

        /// <summary> Write a <c>UInt64[]</c> value to the stream. </summary>
        public void Write(ulong[] l)
        {
            this.Trace("--Wrote ulong array of length {0}", l.Length);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>sbyte[]</c> value to the stream. </summary>
        public void Write(sbyte[] l)
        {
            this.Trace("--Wrote sbyte array of length {0}", l.Length);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>char[]</c> value to the stream. </summary>
        public void Write(char[] l)
        {
            this.Trace("--Wrote char array of length {0}", l.Length);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>bool[]</c> value to the stream. </summary>
        public void Write(bool[] l)
        {
            this.Trace("--Wrote bool array of length {0}", l.Length);
            this.ab.Append(l);
        }

        /// <summary> Write a <c>double[]</c> value to the stream. </summary>
        public void Write(double[] d)
        {
            this.Trace("--Wrote double array of length {0}", d.Length);
            this.ab.Append(d);
        }

        /// <summary> Write a <c>float[]</c> value to the stream. </summary>
        public void Write(float[] f)
        {
            this.Trace("--Wrote float array of length {0}", f.Length);
            this.ab.Append(f);
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


        private StreamWriter trace;

        [Conditional("TRACE_SERIALIZATION")]
        private void Trace(string format, params object[] args)
        {
            if (this.trace == null)
            {
                var path = String.Format("d:\\Trace-{0}.{1}.{2}.txt", DateTime.UtcNow.Hour, DateTime.UtcNow.Minute,
                    DateTime.UtcNow.Ticks);
                Console.WriteLine("Opening trace file at '{0}'", path);
                this.trace = File.CreateText(path);
            }
            this.trace.Write(format, args);
            this.trace.WriteLine(" at offset {0}", this.CurrentOffset);
            this.trace.Flush();
        }
    }
}